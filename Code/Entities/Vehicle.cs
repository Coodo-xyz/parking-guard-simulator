public enum VehicleType
{
	Moto,
	Voiture,
	SUV,
	Camion
}

public sealed class Vehicle : Component
{
	[Property] public VehicleType Type { get; set; } = VehicleType.Voiture;
	[Property] public float ParkingDurationSeconds { get; set; } = 30f;
	[Property] public float MoveSpeed { get; set; } = 280f;
	[Property] public float ModelYawOffset { get; set; } = 90f;
	[Property] public Barrier ExitBarrier { get; set; }

	ParkingSpot spot;
	Vector3 targetPosition;
	float parkingStartTime;
	bool parked;
	bool moving;

	protected override void OnStart()
	{
		if ( Components.Get<BoxCollider>() == null )
		{
			var collider = Components.Create<BoxCollider>();
			collider.Scale = new Vector3( 92, 193, 68 );
			collider.Center = new Vector3( 0, 0, 34 );
			collider.Static = false;
		}
	}

	public void MoveToSpot( ParkingSpot targetSpot )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( targetSpot == null )
		{
			return;
		}

		if ( targetSpot.IsOccupied )
		{
			return;
		}

		spot = targetSpot;
		spot.Occupy( this );
		targetPosition = spot.WorldPosition;
		moving = true;
		parked = false;
	}

	public void Leave()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( spot != null )
		{
			spot.Free();
		}

		if ( GameManager.Instance != null && GameManager.Instance.CashSystem != null )
		{
			var now = Time.Now;
			var elapsedSeconds = now - parkingStartTime;
			if ( elapsedSeconds < 0f )
			{
				elapsedSeconds = 0f;
			}

			var hours = elapsedSeconds / 3600f;
			GameManager.Instance.CashSystem.Collect( hours, Type );
		}

		GameObject.Destroy();
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( moving )
		{
			var delta = targetPosition - WorldPosition;
			delta.z = 0;

			if ( delta.Length <= 8f )
			{
				WorldPosition = targetPosition.WithZ( WorldPosition.z );
				moving = false;
				parked = true;
				parkingStartTime = Time.Now;
				return;
			}

			var direction = delta.Normal;
			WorldPosition += direction * MoveSpeed * Time.Delta;
			WorldRotation = Rotation.LookAt( direction, Vector3.Up ) * Rotation.FromYaw( ModelYawOffset );
			return;
		}

		if ( !parked )
		{
			return;
		}

		if ( ParkingDurationSeconds <= 0f )
		{
			return;
		}

		var elapsed = Time.Now - parkingStartTime;
		if ( elapsed < ParkingDurationSeconds )
		{
			return;
		}

		if ( ExitBarrier != null && !ExitBarrier.IsOpen )
		{
			return;
		}

		Leave();
	}
}
