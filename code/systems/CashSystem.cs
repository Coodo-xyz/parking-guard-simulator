using Sandbox;

public sealed class CashSystem : Component
{
	[Property, ReadOnly] public float Balance { get; set; }
	[Property, ReadOnly] public float TotalEarned { get; private set; }
	[Property, ReadOnly] public int NonPayerCount { get; private set; }

	public bool CanAfford( float amount ) => Balance >= amount;

	public void Earn( float amount )
	{
		Balance += amount;
		TotalEarned += amount;
	}

	public bool Spend( float amount )
	{
		if ( !CanAfford( amount ) ) return false;
		Balance -= amount;
		return true;
	}

	public void RegisterNonPayer()
	{
		NonPayerCount++;
		Log.Info( $"Non-payer! Total: {NonPayerCount}" );
	}

	public void CollectFee( Vehicle vehicle )
	{
		if ( vehicle.WillPay() )
		{
			float fee = vehicle.ComputeFee();
			Earn( fee );
			Log.Info( $"Collected {fee:F2}€ from {vehicle.Type}" );
		}
		else
		{
			RegisterNonPayer();
		}
	}
}
