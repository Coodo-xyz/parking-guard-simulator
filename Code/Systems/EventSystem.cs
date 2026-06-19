public sealed class EventSystem : Component
{
	[Property] public GameObject VehiclePrefab { get; set; }
	[Property] public GameObject VehicleSpawnPoint { get; set; }
	[Property] public float SpawnIntervalSeconds { get; set; } = 15f;
	[Property] public float FirstSpawnDelaySeconds { get; set; } = 1f;
	[Property] public Barrier EntryBarrier { get; set; }
	[Property] public Barrier ExitBarrier { get; set; }

	float nextSpawnTime;
	bool firstSpawnScheduled;

	protected override void OnStart()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		nextSpawnTime = float.MaxValue;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.InGame )
		{
			return;
		}

		if ( !firstSpawnScheduled )
		{
			nextSpawnTime = Time.Now + FirstSpawnDelaySeconds;
			firstSpawnScheduled = true;
		}

		if ( VehiclePrefab == null || Time.Now < nextSpawnTime )
		{
			return;
		}

		if ( !SpawnVehicleIfPossible() )
		{
			return;
		}

		ScheduleNextSpawn();
	}

	bool SpawnVehicleIfPossible()
	{
		if ( TrafficSystem.Instance != null && !TrafficSystem.Instance.CanSpawn() )
		{
			return false;
		}

		var spots = Scene.GetAllComponents<ParkingSpot>();
		var freeSpot = spots.FirstOrDefault( s => !s.IsOccupied );
		if ( freeSpot == null )
		{
			return false;
		}

		var layout = ParkingLotLayout.Instance;
		var spawnPosition = layout != null
			? layout.GetSpawnPosition( 0 )
			: VehicleSpawnPoint != null ? VehicleSpawnPoint.WorldPosition : WorldPosition;
		var spawnRotation = layout != null
			? layout.GetSpawnRotation()
			: VehicleSpawnPoint != null ? VehicleSpawnPoint.WorldRotation : WorldRotation;

		var go = VehiclePrefab.Clone( spawnPosition, spawnRotation );
		go.NetworkSpawn();

		var vehicle = go.Components.Get<Vehicle>();
		if ( vehicle == null )
		{
			return false;
		}

		vehicle.EntryBarrier = EntryBarrier;
		vehicle.ExitBarrier = ExitBarrier;
		vehicle.Type = RollVehicleType();
		vehicle.BeginJourney( freeSpot, 0 );
		return true;
	}

	VehicleType RollVehicleType()
	{
		var roll = Game.Random.Int( 0, 99 );

		if ( roll < 5 )
		{
			return VehicleType.Camion;
		}

		if ( roll < 25 )
		{
			return VehicleType.SUV;
		}

		return VehicleType.Voiture;
	}

	void ScheduleNextSpawn()
	{
		var interval = SpawnIntervalSeconds;

		if ( GameManager.Instance?.PopularitySystem != null )
		{
			var popularity = GameManager.Instance.PopularitySystem.Popularity;
			var factor = 1f;

			if ( popularity > 50f )
			{
				factor = 1f - (popularity - 50f) / 100f * 0.5f;
			}
			else if ( popularity < 50f )
			{
				factor = 1f + (50f - popularity) / 100f * 0.5f;
			}

			if ( factor < 0.25f )
			{
				factor = 0.25f;
			}

			interval *= factor;
		}

		if ( interval < 1f )
		{
			interval = 1f;
		}

		nextSpawnTime = Time.Now + interval;
	}
}
