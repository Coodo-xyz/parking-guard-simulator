using Sandbox;

public sealed class Vehicle : Component
{
	public enum VehicleType { Car, Motorcycle, Van, Truck }
	public enum VehicleBehavior { Normal, NonPayer, Vandal, Thief }

	[Property] public VehicleType Type { get; set; } = VehicleType.Car;
	[Property] public VehicleBehavior Behavior { get; set; } = VehicleBehavior.Normal;

	[Property, ReadOnly] public ParkingSpot AssignedSpot { get; set; }
	[Property, ReadOnly] public TimeSince ParkedSince { get; private set; }

	public float HourlyRate => Type switch
	{
		VehicleType.Motorcycle => 1.5f,
		VehicleType.Car        => 3.0f,
		VehicleType.Van        => 4.5f,
		VehicleType.Truck      => 7.0f,
		_                      => 3.0f
	};

	public void Park( ParkingSpot spot )
	{
		AssignedSpot = spot;
		spot.TryPark( this );
		ParkedSince = 0;
	}

	public float ComputeFee()
	{
		float hours = ParkedSince / 3600f;
		return MathF.Max( HourlyRate, HourlyRate * hours );
	}

	public bool WillPay()
	{
		return Behavior == VehicleBehavior.Normal;
	}

	public void Leave()
	{
		if ( AssignedSpot != null )
		{
			AssignedSpot.Free();
			AssignedSpot = null;
		}

		GameObject.Destroy();
	}
}
