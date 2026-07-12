namespace GameEntry.AutoArmy.Shared;

public sealed class BattleSnapshot
{
    public int StageId { get; set; }

    public int Tick { get; set; }

    public float ElapsedSeconds { get; set; }

    public BattleStatus Status { get; set; } = BattleStatus.Running;

    public BattleOutcome Outcome { get; set; } = BattleOutcome.InProgress;

    public BattleTeam? WinnerTeam { get; set; }

    public bool IsFinished { get; set; }

    public int AllyAliveCount { get; set; }

    public int EnemyAliveCount { get; set; }

    public BattleUnitSnapshot[] Units { get; set; } = [];

    public BattleVisualEvent[] VisualEvents { get; set; } = [];
}

public sealed class BattleUnitSnapshot
{
    public int UnitId { get; set; }

    public int ConfigId { get; set; }

    public BattleTeam Team { get; set; }

    public BattleUnitRole Role { get; set; }

    public BattleUnitKind Kind { get; set; }

    public BattleVisualState VisualState { get; set; } = BattleVisualState.Idle;

    public int Level { get; set; } = 1;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float CurrentHealth { get; set; }

    public float MaxHealth { get; set; }

    public int TargetUnitId { get; set; } = BattleConstants.NoUnitId;

    public bool IsDead { get; set; }
}

public sealed class BattleVisualEvent
{
    public BattleVisualEventType Type { get; set; }

    public int UnitId { get; set; } = BattleConstants.NoUnitId;

    public int TargetUnitId { get; set; } = BattleConstants.NoUnitId;

    public float Value { get; set; }

    public float TimestampSeconds { get; set; }
}
