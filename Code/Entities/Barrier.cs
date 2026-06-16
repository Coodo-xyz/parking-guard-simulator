public sealed class Barrier : Component
{
	[Sync( SyncFlags.FromHost )]
	public bool IsOpen { get; private set; }

	[Property] public float InteractionRange { get; set; } = 128f;
	[Property] public string OpenAnimParameter { get; set; } = "open";
	[Property] public string OpenSequence { get; set; } = "open";
	[Property] public string CloseSequence { get; set; } = "close";
	[Property] public float AnimationPlaybackRate { get; set; } = 1f;
	[Property] public string OpenSound { get; set; } = "sound/hl2_door_open.sound";
	[Property] public string CloseSound { get; set; } = "sound/hl2_door_close.sound";
	[Property] public BoxCollider BlockingCollider { get; set; }

	SkinnedModelRenderer skinnedRenderer;
	bool lastOpenState;

	protected override void OnStart()
	{
		skinnedRenderer = Components.Get<SkinnedModelRenderer>();
		BlockingCollider ??= Components.Get<BoxCollider>();
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

		IsOpen = !IsOpen;
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
