using Sandbox;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }

	[Property] public int StartingMoney { get; set; } = 500;

	public CashSystem Cash { get; private set; }
	public PopularitySystem Popularity { get; private set; }
	public EventSystem Events { get; private set; }

	protected override void OnStart()
	{
		Instance = this;

		Cash = Components.GetOrCreate<CashSystem>();
		Cash.Balance = StartingMoney;

		Popularity = Components.GetOrCreate<PopularitySystem>();
		Events = Components.GetOrCreate<EventSystem>();

		Log.Info( "Parking Guard Simulator — started" );
	}

	protected override void OnUpdate()
	{
		Events.Tick();
	}
}
