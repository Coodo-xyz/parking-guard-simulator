public sealed class TrafficSystem : Component
{
	public static TrafficSystem Instance { get; private set; }

	[Property] public float SafeDistance { get; set; } = 150f;
	[Property] public int MaxEntryQueue { get; set; } = 1;

	Vehicle activeDriver;

	protected override void OnStart()
	{
		if ( Instance != null && Instance != this )
		{
			Enabled = false;
			return;
		}

		Instance = this;
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
		{
			Instance = null;
		}
	}

	public bool CanSpawn()
	{
		return !Vehicle.IsEntryBlocked( Scene );
	}

	public bool TryBecomeDriver( Vehicle vehicle )
	{
		CleanupDriver();

		if ( activeDriver == null )
		{
			activeDriver = vehicle;
			return true;
		}

		return activeDriver == vehicle;
	}

	public void ReleaseDriver( Vehicle vehicle )
	{
		if ( activeDriver == vehicle )
		{
			activeDriver = null;
		}
	}

	public bool IsBlocked( Vehicle vehicle, Vector3 moveDirection )
	{
		CleanupDriver();

		if ( activeDriver != null && activeDriver != vehicle )
		{
			return true;
		}

		var direction = moveDirection.WithZ( 0 );
		if ( direction.Length <= 0.01f )
		{
			direction = vehicle.DriveForward;
		}
		else
		{
			direction = direction.Normal;
		}

		foreach ( var other in Scene.GetAllComponents<Vehicle>() )
		{
			if ( other == vehicle || !other.GameObject.IsValid() )
			{
				continue;
			}

			if ( other.State is VehicleState.Parked or VehicleState.WaitingPayment or VehicleState.WaitingAtExit )
			{
				continue;
			}

			if ( vehicle.IsInEntryZone && other.IsInEntryZone )
			{
				var sameLane = System.MathF.Abs( vehicle.WorldPosition.x - other.WorldPosition.x ) < 40f;
				if ( sameLane && other.WorldPosition.y > vehicle.WorldPosition.y )
				{
					continue;
				}
			}

			var toOther = other.WorldPosition - vehicle.WorldPosition;
			toOther.z = 0;
			var distance = toOther.Length;

			if ( distance < 20f || distance > SafeDistance )
			{
				continue;
			}

			if ( Vector3.Dot( direction, toOther.Normal ) > 0.35f )
			{
				return true;
			}

			if ( distance < SafeDistance * 0.55f )
			{
				return true;
			}
		}

		return false;
	}

	void CleanupDriver()
	{
		if ( activeDriver != null && (!activeDriver.GameObject.IsValid() || activeDriver.State is VehicleState.Parked or VehicleState.WaitingPayment or VehicleState.WaitingAtExit) )
		{
			activeDriver = null;
		}
	}
}
