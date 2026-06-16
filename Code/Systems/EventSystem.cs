public sealed class EventSystem : Component
{
    [Property] public GameObject VehiclePrefab { get; set; }
    [Property] public GameObject VehicleSpawnPoint { get; set; }
    [Property] public float SpawnIntervalSeconds { get; set; } = 15f;
    [Property] public Barrier ExitBarrier { get; set; }

    float nextSpawnTime;

    protected override void OnStart()
    {
        if ( !Networking.IsHost )
        {
            return;
        }

        ScheduleNextSpawn();
    }

    protected override void OnUpdate()
    {
        if ( !Networking.IsHost )
        {
            return;
        }

        if ( GameManager.Instance == null )
        {
            return;
        }

        if ( GameManager.Instance.Phase != GamePhase.InGame )
        {
            return;
        }

        if ( VehiclePrefab == null )
        {
            return;
        }

        if ( Time.Now < nextSpawnTime )
        {
            return;
        }

        SpawnVehicleIfPossible();
        ScheduleNextSpawn();
    }

    void SpawnVehicleIfPossible()
    {
        var spots = Scene.GetAllComponents<ParkingSpot>();
        var freeSpot = spots.FirstOrDefault( s => !s.IsOccupied );
        if ( freeSpot == null )
        {
            return;
        }

        var spawnPosition = VehicleSpawnPoint != null ? VehicleSpawnPoint.WorldPosition : WorldPosition;
        var spawnRotation = VehicleSpawnPoint != null ? VehicleSpawnPoint.WorldRotation : WorldRotation;

        var go = VehiclePrefab.Clone( spawnPosition, spawnRotation );
        go.NetworkSpawn();

        var vehicle = go.Components.Get<Vehicle>();
        if ( vehicle == null )
        {
            return;
        }

        vehicle.ExitBarrier = ExitBarrier;
        vehicle.Type = VehicleType.Voiture;
        vehicle.ParkingDurationSeconds = 30f;
        vehicle.MoveToSpot( freeSpot );
    }

    void ScheduleNextSpawn()
    {
        var interval = SpawnIntervalSeconds;

        if ( GameManager.Instance != null && GameManager.Instance.PopularitySystem != null )
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

