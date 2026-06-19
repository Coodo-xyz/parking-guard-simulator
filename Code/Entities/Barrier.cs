public sealed class Barrier : Component
{
	[Sync( SyncFlags.FromHost )]
	public bool IsOpen { get; private set; }

	[Property] public float InteractionRange { get; set; } = 128f;
	[Property] public string OpenAnimParameter { get; set; } = "open";
	[Property] public string OpenSequence { get; set; } = "open";
	[Property] public string CloseSequence { get; set; } = "close";
	[Property] public float AnimationPlaybackRate { get; set; } = 1f;
	[Property] public string OpenSound { get; set; } = "";
	[Property] public string CloseSound { get; set; } = "";
	[Property] public Collider BlockingCollider { get; set; }

	SkinnedModelRenderer skinnedRenderer;
	bool lastOpenState;

	protected override void OnStart()
	{
		skinnedRenderer = Components.Get<SkinnedModelRenderer>();
		BlockingCollider = ModelColliderUtility.EnsureModelOrBox(
			this,
			skinnedRenderer?.Model,
			new Vector3( 140, 24, 90 ),
			new Vector3( 0, 0, 45 ),
			true );
		lastOpenState = !IsOpen;
		ApplyAnimation();
	}

	protected override void OnFixedUpdate()
	{
		if ( !Input.Pressed( "Use" ) )
		{
			return;
		}

		var controllers = Scene.GetAllComponents<PlayerController>();
		foreach ( var controller in controllers )
		{
			if ( controller.IsProxy )
			{
				continue;
			}

			var distance = (controller.WorldPosition - WorldPosition).Length;
			if ( distance <= InteractionRange )
			{
				RequestToggle();
				break;
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( lastOpenState == IsOpen )
		{
			return;
		}

		lastOpenState = IsOpen;
		ApplyAnimation();
		PlayToggleSound();
	}

	void ApplyAnimation()
	{
		if ( BlockingCollider != null )
		{
			BlockingCollider.Enabled = !IsOpen;
		}

		PlaySequence();
	}

	void PlaySequence()
	{
		if ( skinnedRenderer == null )
		{
			return;
		}

		skinnedRenderer.UseAnimGraph = false;
		skinnedRenderer.Sequence.Name = IsOpen ? OpenSequence : CloseSequence;
		skinnedRenderer.Sequence.Looping = false;
		skinnedRenderer.Sequence.Blending = true;
		skinnedRenderer.PlaybackRate = AnimationPlaybackRate;
	}

	[Rpc.Broadcast]
	void RequestToggle()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( IsOpen && HasVehicleInZone() )
		{
			return;
		}

		IsOpen = !IsOpen;
	}

	bool HasVehicleInZone()
	{
		if ( BlockingCollider == null )
		{
			return false;
		}

		var zoneBounds = BlockingCollider.GetWorldBounds().Grow( 6f );

		foreach ( var vehicle in Scene.GetAllComponents<Vehicle>() )
		{
			if ( !vehicle.GameObject.IsValid() )
			{
				continue;
			}

			var vehicleCollider = vehicle.Components.Get<Collider>();
			if ( vehicleCollider == null )
			{
				continue;
			}

			if ( zoneBounds.Overlaps( vehicleCollider.GetWorldBounds() ) )
			{
				return true;
			}
		}

		return false;
	}

	void PlayToggleSound()
	{
		var soundPath = IsOpen ? OpenSound : CloseSound;
		if ( string.IsNullOrWhiteSpace( soundPath ) )
		{
			return;
		}

		Sound.Play( soundPath, WorldPosition );
	}
}
