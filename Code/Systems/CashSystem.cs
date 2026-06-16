public sealed class CashSystem : Component
{
    [Sync( SyncFlags.FromHost )]
    public int Money { get; private set; }

    [Property] public float BaseRatePerHour { get; set; } = 10f;
    [Property] public float MotoMultiplier { get; set; } = 0.5f;
    [Property] public float VoitureMultiplier { get; set; } = 1f;
    [Property] public float SuvMultiplier { get; set; } = 1.2f;
    [Property] public float CamionMultiplier { get; set; } = 2f;
    [Property] public float MinBillableHours { get; set; } = 0.25f;

    public void Collect( float hours, VehicleType type )
    {
        if ( !Networking.IsHost )
        {
            return;
        }

        if ( hours <= 0f )
        {
            return;
        }

        var clampedHours = hours;
        if ( clampedHours < MinBillableHours )
        {
            clampedHours = MinBillableHours;
        }

        var billedHours = System.MathF.Ceiling( clampedHours );
        var multiplier = GetMultiplier( type );
        var amount = billedHours * BaseRatePerHour * multiplier;

        if ( GameManager.Instance != null && GameManager.Instance.PopularitySystem != null )
        {
            var popularity = GameManager.Instance.PopularitySystem.Popularity;
            if ( popularity < 0f )
            {
                popularity = 0f;
            }

            if ( popularity > 100f )
            {
                popularity = 100f;
            }

            var factor = 1f + (popularity - 50f) / 100f * 0.2f;
            amount *= factor;
        }

        var finalAmount = (int)System.MathF.Round( amount );
        if ( finalAmount <= 0 )
        {
            return;
        }

        Money += finalAmount;
    }

    float GetMultiplier( VehicleType type )
    {
        switch ( type )
        {
            case VehicleType.Moto:
                return MotoMultiplier;
            case VehicleType.SUV:
                return SuvMultiplier;
            case VehicleType.Camion:
                return CamionMultiplier;
            default:
                return VoitureMultiplier;
        }
    }
}

