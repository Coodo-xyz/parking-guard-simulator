public enum VehicleType
{
	Moto,
	Voiture,
	SUV,
	Camion
}

public enum VehicleState
{
	ApproachingEntry,
	WaitingAtEntry,
	Driving,
	Parked,
	Leaving,
	WaitingAtExit,
	WaitingPayment
}

public sealed class Vehicle : Component
{
	[Property] public VehicleType Type { get; set; } = VehicleType.Voiture;
	[Property] public float ParkingDurationSeconds { get; set; } = 30f;
	[Property] public float MoveSpeed { get; set; } = 160f;
	[Property] public float Acceleration { get; set; } = 420f;
	[Property] public float Deceleration { get; set; } = 560f;
	[Property] public float LookaheadMin { get; set; } = 48f;
	[Property] public float LookaheadGain { get; set; } = 0.38f;
	[Property] public float MinTurnRadius { get; set; } = 78f;
	[Property] public float MaxLateralAccel { get; set; } = 520f;
	[Property] public int PathSamplesPerSegment { get; set; } = 22;
	[Property] public float SlowdownDistance { get; set; } = 130f;
	[Property] public float PaymentRange { get; set; } = 140f;
	[Property] public float ArrivalThreshold { get; set; } = 10f;
	[Property] public float BaseScale { get; set; } = 1f;
	[Property] public Barrier EntryBarrier { get; set; }
	[Property] public Barrier ExitBarrier { get; set; }

	readonly List<Vector3> waypoints = new();
	readonly List<Vector3> smoothPath = new();
	readonly List<float> pathDistances = new();
	readonly List<float> waypointPathDistances = new();
	ParkingSpot spot;
	Vector3 targetPosition;
	float parkingStartTime;
	float currentSpeed;
	float pathDistance;
	int pathCursorIndex;
	Rotation driveRotation;
	int waypointIndex;
	VehicleState state;

	public VehicleState State => state;
	public bool IsWaitingPayment => state == VehicleState.WaitingPayment;
	public bool IsInEntryZone => state is VehicleState.ApproachingEntry or VehicleState.WaitingAtEntry;
	public bool IsInExitZone => state is VehicleState.Leaving or VehicleState.WaitingAtExit or VehicleState.WaitingPayment;
	public Vector3 DriveForward => driveRotation.Forward.WithZ( 0 ).Normal;

	public static bool IsEntryBlocked( Scene scene )
	{
		return scene.GetAllComponents<Vehicle>().Any( v => v.IsInEntryZone );
	}

	protected override void OnStart()
	{
		EnsureCollider( VehicleType.Voiture );
		driveRotation = WorldRotation;
	}

	public void BeginJourney( ParkingSpot targetSpot, int entryQueueIndex = 0 )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		ApplyTypeStats();
		spot = targetSpot;
		spot.Occupy( this );
		currentSpeed = 0f;
		state = VehicleState.ApproachingEntry;

		var layout = ParkingLotLayout.Instance;
		if ( layout != null )
		{
			SetPath( layout.BuildPathToSpot( spot, entryQueueIndex ).ToArray() );
		}
		else
		{
			SetPath( GetFallbackEntryWait() );
		}

		AlignToEntryDirection();
	}

	public void ConfirmPayment()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( state != VehicleState.WaitingPayment )
		{
			return;
		}

		Leave();
	}

	void ApplyTypeStats()
	{
		var typeScale = Vector3.One;
		switch ( Type )
		{
			case VehicleType.Moto:
				MoveSpeed = 210f;
				ParkingDurationSeconds = 18f;
				MinTurnRadius = 52f;
				MaxLateralAccel = 680f;
				typeScale = new Vector3( 0.57f, 0.58f, 0.85f );
				break;

			case VehicleType.SUV:
				MoveSpeed = 145f;
				ParkingDurationSeconds = 35f;
				MinTurnRadius = 92f;
				MaxLateralAccel = 420f;
				typeScale = new Vector3( 1.07f, 1.06f, 1.06f );
				break;

			case VehicleType.Camion:
				MoveSpeed = 120f;
				ParkingDurationSeconds = 40f;
				MinTurnRadius = 118f;
				MaxLateralAccel = 320f;
				typeScale = new Vector3( 1.2f, 1.24f, 1.29f );
				break;

			default:
				MoveSpeed = 165f;
				ParkingDurationSeconds = 28f;
				MinTurnRadius = 78f;
				MaxLateralAccel = 520f;
				break;
		}

		WorldScale = typeScale * BaseScale;
		MinTurnRadius *= BaseScale;
		EnsureCollider( Type );
	}

	void EnsureCollider( VehicleType vehicleType )
	{
		var renderer = Components.Get<ModelRenderer>();
		var visualModel = renderer?.Model;
		var collisionModel = VehicleCollisionModelCache.GetCollisionModel( visualModel );
		var fallback = GetColliderFallback( visualModel, vehicleType );
		ModelColliderUtility.EnsureModelOrBox( this, collisionModel, fallback.Scale, fallback.Center, false );
	}

	static (Vector3 Scale, Vector3 Center) GetColliderFallback( Model model, VehicleType vehicleType )
	{
		if ( model != null && model.IsValid )
		{
			var bounds = model.Bounds;
			return (bounds.Size, bounds.Center);
		}

		return vehicleType switch
		{
			VehicleType.SUV => (new Vector3( 98, 205, 72 ), new Vector3( 0, 0, 36 )),
			VehicleType.Camion => (new Vector3( 110, 240, 88 ), new Vector3( 0, 0, 44 )),
			_ => (new Vector3( 92, 193, 68 ), new Vector3( 0, 0, 34 ))
		};
	}

	void SetPath( params Vector3[] points )
	{
		waypoints.Clear();
		waypoints.AddRange( points );
		waypointIndex = 0;
		targetPosition = waypoints[0];
		pathDistance = 0f;
		pathCursorIndex = 0;
		currentSpeed = 0f;
		RebuildSmoothPath();
	}

	void RebuildSmoothPath()
	{
		VehiclePathBuilder.Build( waypoints, PathSamplesPerSegment, smoothPath, pathDistances, waypointPathDistances );
		pathCursorIndex = VehiclePathBuilder.FindClosestIndex( WorldPosition.WithZ( 0 ), smoothPath, pathCursorIndex );
		if ( pathCursorIndex < pathDistances.Count )
		{
			pathDistance = System.MathF.Max( pathDistance, pathDistances[pathCursorIndex] );
		}
	}

	Vector3 GetFallbackEntryWait()
	{
		if ( EntryBarrier != null )
		{
			return EntryBarrier.WorldPosition + new Vector3( 0, 125f, 0 );
		}

		return WorldPosition;
	}

	void Leave()
	{
		TrafficSystem.Instance?.ReleaseDriver( this );

		if ( spot != null )
		{
			spot.Free();
			spot = null;
		}

		if ( GameManager.Instance?.CashSystem != null )
		{
			var elapsedSeconds = Time.Now - parkingStartTime;
			if ( elapsedSeconds < 0f )
			{
				elapsedSeconds = 0f;
			}

			GameManager.Instance.CashSystem.Collect( elapsedSeconds / 3600f, Type );
		}

		GameObject.Destroy();
	}

	void TryCollectPayment()
	{
		if ( !Input.Pressed( "Use" ) )
		{
			return;
		}

		foreach ( var controller in Scene.GetAllComponents<PlayerController>() )
		{
			if ( controller.IsProxy )
			{
				continue;
			}

			if ( (controller.WorldPosition - WorldPosition).Length <= PaymentRange )
			{
				ConfirmPayment();
				return;
			}
		}
	}

	void ApplyDriveRotation( Vector3 forward )
	{
		forward.z = 0;
		if ( forward.Length <= 0.01f )
		{
			return;
		}

		driveRotation = Rotation.LookAt( forward.Normal, Vector3.Up );
		WorldRotation = driveRotation;
		ModelColliderUtility.SyncTransform( this, WorldPosition, WorldRotation );
	}

	void ApplyParkedRotation()
	{
		var forward = spot != null && spot.RowY > 0f
			? new Vector3( 0, 1, 0 )
			: new Vector3( 0, -1, 0 );

		ApplyDriveRotation( forward );
		currentSpeed = 0f;
	}

	int GetEntryQueueIndex()
	{
		var queue = Scene.GetAllComponents<Vehicle>()
			.Where( v => v.IsInEntryZone && v.GameObject.IsValid() )
			.OrderByDescending( v => v.WorldPosition.y )
			.ToList();

		return queue.IndexOf( this );
	}

	void UpdateEntryQueueTarget()
	{
		if ( !IsInEntryZone || waypoints.Count == 0 || ParkingLotLayout.Instance == null )
		{
			return;
		}

		var queueTarget = ParkingLotLayout.Instance.GetEntryQueuePosition( GetEntryQueueIndex() );
		if ( (waypoints[0] - queueTarget).Length <= 1f )
		{
			return;
		}

		waypoints[0] = queueTarget;

		if ( waypointIndex == 0 )
		{
			targetPosition = queueTarget;
		}

		RebuildSmoothPath();
	}

	bool IsFirstInEntryQueue()
	{
		return GetEntryQueueIndex() == 0;
	}

	bool IsBarrierBlocking( Vector3 moveDirection )
	{
		var layout = ParkingLotLayout.Instance;
		moveDirection.z = 0;
		if ( layout == null || moveDirection.Length <= 0.01f )
		{
			return false;
		}

		moveDirection = moveDirection.Normal;
		if ( moveDirection.y >= -0.35f )
		{
			return false;
		}

		if ( state == VehicleState.Driving && EntryBarrier != null && !EntryBarrier.IsOpen )
		{
			if ( WorldPosition.y > layout.EntryBarrierY )
			{
				return true;
			}
		}

		if ( state == VehicleState.Leaving && ExitBarrier != null && !ExitBarrier.IsOpen )
		{
			if ( WorldPosition.y > layout.ExitBarrierY )
			{
				return true;
			}
		}

		return false;
	}

	bool CanMoveForward()
	{
		var forward = driveRotation.Forward.WithZ( 0 );
		if ( forward.Length <= 0.01f )
		{
			return false;
		}

		forward = forward.Normal;

		if ( IsBarrierBlocking( forward ) )
		{
			return false;
		}

		if ( TrafficSystem.Instance == null )
		{
			return true;
		}

		if ( state is VehicleState.Driving or VehicleState.Leaving )
		{
			if ( !TrafficSystem.Instance.TryBecomeDriver( this ) )
			{
				return false;
			}
		}

		return !TrafficSystem.Instance.IsBlocked( this, forward );
	}

	static float MoveTowards( float current, float target, float maxDelta )
	{
		if ( System.MathF.Abs( target - current ) <= maxDelta )
		{
			return target;
		}

		return current + System.MathF.Sign( target - current ) * maxDelta;
	}

	static float Clamp( float value, float min, float max )
	{
		if ( value < min )
		{
			return min;
		}

		if ( value > max )
		{
			return max;
		}

		return value;
	}

	float GetPathEndDistance()
	{
		if ( pathDistances.Count == 0 )
		{
			return 0f;
		}

		return pathDistances[^1];
	}

	float GetWaypointPathDistance()
	{
		if ( waypointIndex >= waypointPathDistances.Count )
		{
			return GetPathEndDistance();
		}

		return waypointPathDistances[waypointIndex];
	}

	float GetCurvatureSpeedLimit( float distance )
	{
		var curvature = VehiclePathBuilder.EstimateCurvature( distance, smoothPath, pathDistances, 22f );
		curvature = System.MathF.Max( curvature, 1f / (MinTurnRadius * 4f) );
		return System.MathF.Sqrt( MaxLateralAccel / curvature );
	}

	float GetTargetSpeed()
	{
		if ( smoothPath.Count < 2 )
		{
			return 0f;
		}

		var waypointDistance = GetWaypointPathDistance();
		var remaining = waypointDistance - pathDistance;
		var targetSpeed = MoveSpeed;

		if ( remaining < SlowdownDistance )
		{
			var blend = System.MathF.Max( 0f, remaining ) / SlowdownDistance;
			targetSpeed = MoveSpeed * (0.15f + 0.85f * blend );
		}

		for ( var offset = 0f; offset <= 90f; offset += 30f )
		{
			targetSpeed = System.MathF.Min( targetSpeed, GetCurvatureSpeedLimit( pathDistance + offset ) );
		}

		return targetSpeed;
	}

	void AlignToPathTangent()
	{
		if ( smoothPath.Count < 2 )
		{
			return;
		}

		var ahead = VehiclePathBuilder.SampleAtDistance( pathDistance + 24f, smoothPath, pathDistances );
		var tangent = ahead - WorldPosition.WithZ( 0 );
		tangent.z = 0;
		if ( tangent.Length <= 0.01f )
		{
			return;
		}

		driveRotation = Rotation.LookAt( tangent.Normal, Vector3.Up );
		WorldRotation = driveRotation;
		ModelColliderUtility.SyncTransform( this, WorldPosition, WorldRotation );
	}

	void AlignToEntryDirection()
	{
		Vector3 direction;

		if ( waypoints.Count > 0 )
		{
			direction = (waypoints[0] - WorldPosition).WithZ( 0 );
		}
		else
		{
			direction = Vector3.Zero;
		}

		if ( direction.Length <= 0.01f && smoothPath.Count > 1 )
		{
			direction = (smoothPath[1] - smoothPath[0]).WithZ( 0 );
		}

		if ( direction.Length <= 0.01f )
		{
			direction = new Vector3( 0, -1, 0 );
		}

		driveRotation = Rotation.LookAt( direction.Normal, Vector3.Up );
		WorldRotation = driveRotation;
		ModelColliderUtility.SyncTransform( this, WorldPosition, WorldRotation );
	}

	void DrivePath()
	{
		if ( smoothPath.Count < 2 || pathDistances.Count < 2 )
		{
			return;
		}

		var position = WorldPosition.WithZ( 0 );
		pathCursorIndex = VehiclePathBuilder.FindClosestIndex( position, smoothPath, pathCursorIndex );
		pathDistance = System.MathF.Max( pathDistance, pathDistances[pathCursorIndex] );

		var waypointDistance = GetWaypointPathDistance();
		if ( pathDistance >= waypointDistance - 2f )
		{
			currentSpeed = MoveTowards( currentSpeed, 0f, Deceleration * Time.Delta );
			return;
		}

		var lookahead = LookaheadMin + currentSpeed * LookaheadGain;
		var lookaheadDistance = System.MathF.Min( pathDistance + lookahead, waypointDistance );
		var lookaheadPoint = VehiclePathBuilder.SampleAtDistance( lookaheadDistance, smoothPath, pathDistances );

		var toTarget = lookaheadPoint - position;
		toTarget.z = 0;
		if ( toTarget.Length <= 0.01f )
		{
			currentSpeed = MoveTowards( currentSpeed, 0f, Deceleration * Time.Delta );
			return;
		}

		toTarget = toTarget.Normal;
		var forward = driveRotation.Forward.WithZ( 0 ).Normal;
		var cross = forward.x * toTarget.y - forward.y * toTarget.x;
		var dot = Clamp( Vector3.Dot( forward, toTarget ), -1f, 1f );
		var alpha = System.MathF.Atan2( cross, dot );
		var curvature = 2f * System.MathF.Sin( alpha ) / System.MathF.Max( lookahead, 1f );
		var maxCurvature = 1f / MinTurnRadius;
		curvature = Clamp( curvature, -maxCurvature, maxCurvature );

		var targetSpeed = GetTargetSpeed();
		if ( !CanMoveForward() )
		{
			targetSpeed = 0f;
		}

		var deltaRate = targetSpeed > currentSpeed ? Acceleration : Deceleration;
		currentSpeed = MoveTowards( currentSpeed, targetSpeed, deltaRate * Time.Delta );

		if ( currentSpeed <= 0.5f )
		{
			return;
		}

		var turnDegrees = curvature * currentSpeed * Time.Delta * (180f / System.MathF.PI);
		driveRotation *= Rotation.FromAxis( Vector3.Up, turnDegrees );
		WorldRotation = driveRotation;

		var step = currentSpeed * Time.Delta;
		var nextPosition = WorldPosition + driveRotation.Forward.WithZ( 0 ).Normal * step;
		pathDistance += step;
		pathDistance = System.MathF.Min( pathDistance, waypointDistance );
		WorldPosition = nextPosition.WithZ( WorldPosition.z );
		ModelColliderUtility.SyncTransform( this, WorldPosition, WorldRotation );
	}

	bool AdvanceWaypoint()
	{
		waypointIndex++;

		if ( waypointIndex >= waypoints.Count )
		{
			return true;
		}

		targetPosition = waypoints[waypointIndex];
		pathCursorIndex = VehiclePathBuilder.FindClosestIndex( WorldPosition.WithZ( 0 ), smoothPath, pathCursorIndex );
		if ( pathCursorIndex < pathDistances.Count )
		{
			pathDistance = pathDistances[pathCursorIndex];
		}

		AlignToPathTangent();
		return false;
	}

	bool HasReachedTarget()
	{
		if ( waypointIndex < waypointPathDistances.Count && pathDistance >= waypointPathDistances[waypointIndex] - ArrivalThreshold )
		{
			return true;
		}

		var delta = targetPosition - WorldPosition;
		delta.z = 0;
		return delta.Length <= ArrivalThreshold;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		switch ( state )
		{
			case VehicleState.ApproachingEntry:
				UpdateEntryQueueTarget();
				DrivePath();
				if ( HasReachedTarget() )
				{
					state = VehicleState.WaitingAtEntry;
					currentSpeed = 0f;
				}

				break;

			case VehicleState.WaitingAtEntry:
				UpdateEntryQueueTarget();

				if ( !HasReachedTarget() )
				{
					state = VehicleState.ApproachingEntry;
					waypointIndex = 0;
					targetPosition = waypoints[0];
					break;
				}

				if ( !IsFirstInEntryQueue() )
				{
					break;
				}

				if ( EntryBarrier != null && !EntryBarrier.IsOpen )
				{
					break;
				}

				if ( TrafficSystem.Instance != null && !TrafficSystem.Instance.TryBecomeDriver( this ) )
				{
					break;
				}

				state = VehicleState.Driving;
				if ( waypoints.Count > 1 )
				{
					waypointIndex = 1;
					targetPosition = waypoints[1];
					AlignToPathTangent();
				}

				break;

			case VehicleState.Driving:
				DrivePath();
				if ( !HasReachedTarget() )
				{
					break;
				}

				if ( AdvanceWaypoint() )
				{
					state = VehicleState.Parked;
					currentSpeed = 0f;

					if ( spot != null && ParkingLotLayout.Instance != null )
					{
						WorldPosition = ParkingLotLayout.Instance.GetParkingPosition( spot ).WithZ( WorldPosition.z );
						ModelColliderUtility.SyncTransform( this, WorldPosition, WorldRotation );
					}

					ApplyParkedRotation();
					parkingStartTime = Time.Now;
					TrafficSystem.Instance?.ReleaseDriver( this );
				}

				break;

			case VehicleState.Parked:
				if ( ParkingDurationSeconds > 0f && Time.Now - parkingStartTime < ParkingDurationSeconds )
				{
					break;
				}

				state = VehicleState.Leaving;
				var layout = ParkingLotLayout.Instance;
				if ( layout != null )
				{
					SetPath( layout.BuildPathToExit( WorldPosition ).ToArray() );
				}

				break;

			case VehicleState.Leaving:
				if ( TrafficSystem.Instance != null && !TrafficSystem.Instance.TryBecomeDriver( this ) )
				{
					break;
				}

				DrivePath();

				if ( !HasReachedTarget() )
				{
					break;
				}

				if ( !AdvanceWaypoint() )
				{
					break;
				}

				if ( ExitBarrier != null && !ExitBarrier.IsOpen )
				{
					state = VehicleState.WaitingAtExit;
					currentSpeed = 0f;
					TrafficSystem.Instance?.ReleaseDriver( this );
					break;
				}

				state = VehicleState.WaitingPayment;
				currentSpeed = 0f;
				TrafficSystem.Instance?.ReleaseDriver( this );
				break;

			case VehicleState.WaitingAtExit:
				if ( ExitBarrier != null && !ExitBarrier.IsOpen )
				{
					break;
				}

				state = VehicleState.WaitingPayment;
				break;

			case VehicleState.WaitingPayment:
				TryCollectPayment();
				break;
		}
	}
}
