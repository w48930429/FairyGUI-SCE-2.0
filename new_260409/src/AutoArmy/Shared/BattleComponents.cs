namespace GameEntry.AutoArmy.Shared;

public sealed class BattleTransform2DComponent
{
    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float DirectionY { get; set; }
}

public sealed class BattleHealthComponent
{
    public float CurrentHealth { get; set; }

    public float MaxHealth { get; set; }

    public bool IsDead { get; set; }
}

public sealed class BattleStatsComponent
{
    public float Attack { get; set; }

    public float Defense { get; set; }

    public float AttackRange { get; set; }

    public float MoveSpeed { get; set; }

    public float AttackIntervalSeconds { get; set; }

    public float SkillPower { get; set; }
}

public sealed class BattleAttackComponent
{
    public float CooldownRemainingSeconds { get; set; }

    public float AttackIntervalSeconds { get; set; }

    public int CurrentTargetUnitId { get; set; } = BattleConstants.NoUnitId;
}

public sealed class BattleMovementComponent
{
    public float DirectionY { get; set; }

    public float StopDistance { get; set; }

    public bool IsAdvancing { get; set; } = true;
}

public sealed class BattleTargetingComponent
{
    public float SearchRadius { get; set; }

    public BattleTargetPreference Preference { get; set; } = BattleTargetPreference.Nearest;

    public int CurrentTargetUnitId { get; set; } = BattleConstants.NoUnitId;
}

public sealed class BattleRoleComponent
{
    public int ConfigId { get; set; }

    public BattleTeam Team { get; set; }

    public BattleUnitRole Role { get; set; }

    public BattleUnitKind Kind { get; set; }

    public int Level { get; set; } = 1;
}

public sealed class PassiveSkillComponent
{
    public string SkillId { get; set; } = string.Empty;

    public float Potency { get; set; } = 1f;

    public bool IsEnabled { get; set; } = true;
}

public sealed class AutoCastSkillComponent
{
    public string SkillId { get; set; } = string.Empty;

    public float Potency { get; set; } = 1f;

    public float CooldownSeconds { get; set; } = 5f;

    public float CooldownRemainingSeconds { get; set; }

    public float CastRange { get; set; } = 3f;

    public BattleSkillTargetRule TargetRule { get; set; } = BattleSkillTargetRule.CurrentTarget;
}

public sealed class BattleVisualStateComponent
{
    public BattleVisualState State { get; set; } = BattleVisualState.Idle;

    public float ElapsedSeconds { get; set; }
}
