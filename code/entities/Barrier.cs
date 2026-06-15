using Sandbox;

public sealed class Barrier : Component
{
	public enum BarrierMode { Manual, Automatic }

	[Property] public BarrierMode Mode { get; set; } = BarrierMode.Manual;
	[Property, ReadOnly] public bool IsOpen { get; private set; }

	public void Open()
	{
		IsOpen = true;
		// TODO: animate barrier arm up
	}

	public void Close()
	{
		IsOpen = false;
		// TODO: animate barrier arm down
	}

	public void Toggle() => _ = IsOpen ? Close() : Open();

	/// <summary>Called by player action (E key / button press)</summary>
	public void PlayerInteract()
	{
		if ( Mode == BarrierMode.Manual )
			Toggle();
	}
}
