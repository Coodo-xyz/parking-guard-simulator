using Sandbox;

public sealed class ParkingSpot : Component
{
	public enum SpotState { Free, Occupied, Degraded, Dirty }

	[Property, ReadOnly] public SpotState State { get; private set; } = SpotState.Free;
	[Property, ReadOnly] public Vehicle OccupiedBy { get; private set; }

	public bool IsAvailable => State == SpotState.Free;

	public bool TryPark( Vehicle vehicle )
	{
		if ( !IsAvailable ) return false;

		OccupiedBy = vehicle;
		State = SpotState.Occupied;
		return true;
	}

	public void Free()
	{
		OccupiedBy = null;
		// Each use slightly degrades the spot
		State = Game.Random.Float() < 0.15f ? SpotState.Dirty : SpotState.Free;
	}

	public void Clean()
	{
		if ( State == SpotState.Dirty || State == SpotState.Degraded )
			State = SpotState.Free;
	}
}
