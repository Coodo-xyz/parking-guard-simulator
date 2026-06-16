public enum GamePhase
{
    WaitingForPlayers,
    InGame,
    GameOver
}

public sealed class GameManager : Component
{
    public static GameManager Instance { get; private set; }

    [Sync( SyncFlags.FromHost )]
    public GamePhase Phase { get; private set; } = GamePhase.WaitingForPlayers;

    [Property] public CashSystem CashSystem { get; set; }
    [Property] public PopularitySystem PopularitySystem { get; set; }
    [Property] public EventSystem EventSystem { get; set; }

    [Property] public float WaitBeforeStartSeconds { get; set; } = 3f;
    [Property] public float GameDurationSeconds { get; set; } = 300f;

    float startTime;
    float gameEndTime;
    bool started;

    protected override void OnStart()
    {
        if ( Instance != null && Instance != this )
        {
            Enabled = false;
            return;
        }

        Instance = this;
        ResolveSystems();

        if ( !Networking.IsHost )
        {
            return;
        }

        startTime = Time.Now + WaitBeforeStartSeconds;
        gameEndTime = startTime + GameDurationSeconds;
        Phase = GamePhase.WaitingForPlayers;
        started = true;
    }

    void ResolveSystems()
    {
        if ( CashSystem == null )
        {
            CashSystem = Scene.GetAllComponents<CashSystem>().FirstOrDefault();
        }

        if ( PopularitySystem == null )
        {
            PopularitySystem = Scene.GetAllComponents<PopularitySystem>().FirstOrDefault();
        }

        if ( EventSystem == null )
        {
            EventSystem = Scene.GetAllComponents<EventSystem>().FirstOrDefault();
        }
    }

    protected override void OnUpdate()
    {
        if ( !Networking.IsHost )
        {
            return;
        }

        if ( !started )
        {
            return;
        }

        if ( Phase == GamePhase.WaitingForPlayers && Time.Now >= startTime )
        {
            Phase = GamePhase.InGame;
        }

        if ( Phase == GamePhase.InGame && Time.Now >= gameEndTime )
        {
            Phase = GamePhase.GameOver;
        }
    }
}

