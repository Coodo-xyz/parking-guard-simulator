public sealed class ParkingSpot : Component
{
	[Sync( SyncFlags.FromHost )]
	public bool IsOccupied { get; private set; }

	[Sync( SyncFlags.FromHost )]
	public bool IsClean { get; private set; } = true;

	[Sync( SyncFlags.FromHost )]
	public float DegradationLevel { get; private set; }

	[Property] public float MaxDegradation { get; set; } = 3f;
	[Property] public float OccupiedDegradationPerSecond { get; set; } = 0.05f;
	[Property] public float DirtyThreshold { get; set; } = 1.5f;

	Vehicle currentVehicle;
	ModelRenderer renderer;

	protected override void OnStart()
	{
		renderer = Components.Get<ModelRenderer>();
	}

	public void Occupy( Vehicle vehicle )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( IsOccupied )
		{
			return;
		}

		currentVehicle = vehicle;
		IsOccupied = true;
	}

	public void Free()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( !IsOccupied )
		{
			return;
		}

		currentVehicle = null;
		IsOccupied = false;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( IsOccupied )
		{
			DegradationLevel += OccupiedDegradationPerSecond * Time.Delta;
			if ( DegradationLevel > MaxDegradation )
			{
				DegradationLevel = MaxDegradation;
			}
		}

		IsClean = DegradationLevel < DirtyThreshold;
	}

	protected override void OnUpdate()
	{
		if ( renderer == null )
		{
			return;
		}

		if ( IsOccupied )
		{
			renderer.Tint = new Color( 0.85f, 0.45f, 0.15f );
			return;
		}

		if ( !IsClean )
		{
			renderer.Tint = new Color( 0.45f, 0.4f, 0.32f );
			return;
		}

		renderer.Tint = new Color( 0.92f, 0.92f, 0.88f );
	}
}
