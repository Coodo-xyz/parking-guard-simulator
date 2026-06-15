using Sandbox;

public sealed class EventSystem : Component
{
	[Property] public float EventIntervalMin { get; set; } = 60f;
	[Property] public float EventIntervalMax { get; set; } = 180f;

	private TimeUntil _nextEvent;

	protected override void OnStart()
	{
		ScheduleNextEvent();
	}

	public void Tick()
	{
		if ( _nextEvent )
		{
			TriggerRandomEvent();
			ScheduleNextEvent();
		}
	}

	private void ScheduleNextEvent()
	{
		_nextEvent = Game.Random.Float( EventIntervalMin, EventIntervalMax );
	}

	private void TriggerRandomEvent()
	{
		float roll = Game.Random.Float();

		if ( roll < 0.25f )      SpawnThief();
		else if ( roll < 0.45f ) SpawnVandal();
		else if ( roll < 0.65f ) SpawnNonPayer();
		else if ( roll < 0.75f ) SpawnAbandonedCar();
		// else: nothing happens this cycle
	}

	private void SpawnThief()       => Log.Info( "[Event] Thief spotted!" );
	private void SpawnVandal()      => Log.Info( "[Event] Vandal incoming!" );
	private void SpawnNonPayer()    => Log.Info( "[Event] Non-payer vehicle spawned" );
	private void SpawnAbandonedCar() => Log.Info( "[Event] Abandoned car appeared" );
}
