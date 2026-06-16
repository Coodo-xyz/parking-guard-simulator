public sealed class PopularitySystem : Component
{
    [Sync( SyncFlags.FromHost )]
    public float Popularity { get; private set; } = 50f;

    [Property] public float ChangeSpeedPerSecond { get; set; } = 10f;
    [Property] public float DirtyPenalty { get; set; } = 30f;
    [Property] public float DegradationPenaltyFactor { get; set; } = 50f;

    protected override void OnFixedUpdate()
    {
        if ( !Networking.IsHost )
        {
            return;
        }

        var spots = Scene.GetAllComponents<ParkingSpot>().ToList();
        if ( spots.Count == 0 )
        {
            return;
        }

        float totalDegradation = 0f;
        int dirtyCount = 0;

        foreach ( var spot in spots )
        {
            totalDegradation += spot.DegradationLevel;
            if ( !spot.IsClean )
            {
                dirtyCount++;
            }
        }

        var averageDegradation = totalDegradation / spots.Count;
        var dirtyRatio = spots.Count > 0 ? (float)dirtyCount / spots.Count : 0f;

        var target = 100f;
        target -= averageDegradation * DegradationPenaltyFactor;
        target -= dirtyRatio * DirtyPenalty;

        if ( target < 0f )
        {
            target = 0f;
        }

        if ( target > 100f )
        {
            target = 100f;
        }

        var delta = target - Popularity;
        var maxStep = ChangeSpeedPerSecond * Time.Delta;
        if ( System.MathF.Abs( delta ) <= maxStep )
        {
            Popularity = target;
        }
        else
        {
            Popularity += System.MathF.Sign( delta ) * maxStep;
        }
    }
}

