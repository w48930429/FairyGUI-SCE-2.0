using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public sealed class BattleUnitDefinition
{
    public int ConfigId { get; set; }

    public BattleTeam Team { get; set; }

    public BattleUnitRole Role { get; set; }

    public BattleUnitKind Kind { get; set; }

    public int Level { get; set; } = 1;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float MaxHealth { get; set; }

    public float TargetSearchRadius { get; set; } = 20f;

    public float InitialAttackCooldownSeconds { get; set; }

    public float InitialSkillCooldownSeconds { get; set; }

    public BattleStatsComponent Stats { get; set; } = new();

    public PassiveSkillComponent? PassiveSkill { get; set; }

    public AutoCastSkillComponent? AutoCastSkill { get; set; }
}

public sealed class BattleWorld
{
    private int _nextUnitId = 1;
    private readonly List<BattleVisualEvent> _visualEvents = [];

    public BattleWorld(RoleAdvantageTable? roleAdvantageTable = null)
    {
        RoleAdvantageTable = roleAdvantageTable ?? RoleAdvantageTable.CreateDefault();
    }

    public RoleAdvantageTable RoleAdvantageTable { get; }

    public Dictionary<int, BattleTransform2DComponent> Transforms { get; } = new();

    public Dictionary<int, BattleHealthComponent> Health { get; } = new();

    public Dictionary<int, BattleStatsComponent> BaseStats { get; } = new();

    public Dictionary<int, BattleStatsComponent> Stats { get; } = new();

    public Dictionary<int, BattleAttackComponent> Attacks { get; } = new();

    public Dictionary<int, BattleMovementComponent> Movement { get; } = new();

    public Dictionary<int, BattleTargetingComponent> Targeting { get; } = new();

    public Dictionary<int, BattleRoleComponent> Roles { get; } = new();

    public Dictionary<int, PassiveSkillComponent> PassiveSkills { get; } = new();

    public Dictionary<int, AutoCastSkillComponent> AutoCastSkills { get; } = new();

    public Dictionary<int, BattleVisualStateComponent> VisualStates { get; } = new();

    public int Tick { get; private set; }

    public float ElapsedSeconds { get; private set; }

    public int StageId { get; set; } = 1;

    public BattleOutcome Outcome { get; set; } = BattleOutcome.InProgress;

    public int SpawnUnit(BattleUnitDefinition definition)
    {
        var unitId = _nextUnitId++;
        var baseStats = CloneStats(definition.Stats);

        Transforms[unitId] = new BattleTransform2DComponent
        {
            PositionX = definition.PositionX,
            PositionY = definition.PositionY,
            DirectionY = GetDefaultDirection(definition.Team),
        };
        Health[unitId] = new BattleHealthComponent
        {
            CurrentHealth = definition.MaxHealth,
            MaxHealth = definition.MaxHealth,
        };
        BaseStats[unitId] = CloneStats(baseStats);
        Stats[unitId] = CloneStats(baseStats);
        Attacks[unitId] = new BattleAttackComponent
        {
            AttackIntervalSeconds = definition.Stats.AttackIntervalSeconds,
            CooldownRemainingSeconds = definition.InitialAttackCooldownSeconds,
        };
        Movement[unitId] = new BattleMovementComponent
        {
            DirectionY = GetDefaultDirection(definition.Team),
            StopDistance = definition.Stats.AttackRange,
        };
        Targeting[unitId] = new BattleTargetingComponent
        {
            SearchRadius = definition.TargetSearchRadius,
        };
        Roles[unitId] = new BattleRoleComponent
        {
            ConfigId = definition.ConfigId,
            Team = definition.Team,
            Role = definition.Role,
            Kind = definition.Kind,
            Level = definition.Level,
        };
        VisualStates[unitId] = new BattleVisualStateComponent();

        if (definition.PassiveSkill is not null)
        {
            PassiveSkills[unitId] = new PassiveSkillComponent
            {
                SkillId = definition.PassiveSkill.SkillId,
                Potency = definition.PassiveSkill.Potency,
                IsEnabled = definition.PassiveSkill.IsEnabled,
            };
        }

        if (definition.AutoCastSkill is not null)
        {
            AutoCastSkills[unitId] = new AutoCastSkillComponent
            {
                SkillId = definition.AutoCastSkill.SkillId,
                Potency = definition.AutoCastSkill.Potency,
                CooldownSeconds = definition.AutoCastSkill.CooldownSeconds,
                CooldownRemainingSeconds = definition.InitialSkillCooldownSeconds,
                CastRange = definition.AutoCastSkill.CastRange,
                TargetRule = definition.AutoCastSkill.TargetRule,
            };
        }

        return unitId;
    }

    public bool IsAlive(int unitId)
    {
        return Health.TryGetValue(unitId, out var health) && !health.IsDead;
    }

    public int GetAliveCount(BattleTeam team)
    {
        return GetAliveUnitIds(team).Count();
    }

    public IEnumerable<int> GetAliveUnitIds(BattleTeam? team = null)
    {
        foreach (var unitId in Roles.Keys.OrderBy(static id => id))
        {
            if (!IsAlive(unitId))
            {
                continue;
            }

            if (team is not null && Roles[unitId].Team != team.Value)
            {
                continue;
            }

            yield return unitId;
        }
    }

    public void AdvanceTime(float deltaTimeSeconds)
    {
        Tick++;
        ElapsedSeconds += deltaTimeSeconds;
    }

    public void ClearVisualEvents()
    {
        _visualEvents.Clear();
    }

    public void AddVisualEvent(BattleVisualEventType type, int unitId, int targetUnitId = BattleConstants.NoUnitId, float value = 0f)
    {
        _visualEvents.Add(new BattleVisualEvent
        {
            Type = type,
            UnitId = unitId,
            TargetUnitId = targetUnitId,
            Value = value,
            TimestampSeconds = ElapsedSeconds,
        });
    }

    public float GetDefaultDirection(BattleTeam team)
    {
        return team == BattleTeam.Enemy ? 1f : -1f;
    }

    public BattleSnapshot CreateSnapshot()
    {
        var unitSnapshots = Roles.Keys
            .OrderBy(static id => id)
            .Select(CreateUnitSnapshot)
            .ToArray();

        return new BattleSnapshot
        {
            StageId = StageId,
            Tick = Tick,
            ElapsedSeconds = ElapsedSeconds,
            Status = Outcome == BattleOutcome.InProgress ? BattleStatus.Running : BattleStatus.Finished,
            Outcome = Outcome,
            WinnerTeam = GetWinnerTeam(Outcome),
            IsFinished = Outcome != BattleOutcome.InProgress,
            AllyAliveCount = GetAliveCount(BattleTeam.Ally),
            EnemyAliveCount = GetAliveCount(BattleTeam.Enemy),
            Units = unitSnapshots,
            VisualEvents = _visualEvents
                .Select(static visualEvent => new BattleVisualEvent
                {
                    Type = visualEvent.Type,
                    UnitId = visualEvent.UnitId,
                    TargetUnitId = visualEvent.TargetUnitId,
                    Value = visualEvent.Value,
                    TimestampSeconds = visualEvent.TimestampSeconds,
                })
                .ToArray(),
        };
    }

    private static BattleTeam? GetWinnerTeam(BattleOutcome outcome)
    {
        return outcome switch
        {
            BattleOutcome.Victory => BattleTeam.Ally,
            BattleOutcome.Defeat => BattleTeam.Enemy,
            _ => null,
        };
    }

    private BattleUnitSnapshot CreateUnitSnapshot(int unitId)
    {
        var role = Roles[unitId];
        var transform = Transforms[unitId];
        var health = Health[unitId];
        var targeting = Targeting[unitId];
        var visual = VisualStates[unitId];

        return new BattleUnitSnapshot
        {
            UnitId = unitId,
            ConfigId = role.ConfigId,
            Team = role.Team,
            Role = role.Role,
            Kind = role.Kind,
            VisualState = visual.State,
            Level = role.Level,
            PositionX = transform.PositionX,
            PositionY = transform.PositionY,
            CurrentHealth = health.CurrentHealth,
            MaxHealth = health.MaxHealth,
            TargetUnitId = targeting.CurrentTargetUnitId,
            IsDead = health.IsDead,
        };
    }

    private static BattleStatsComponent CloneStats(BattleStatsComponent source)
    {
        return new BattleStatsComponent
        {
            Attack = source.Attack,
            Defense = source.Defense,
            AttackRange = source.AttackRange,
            MoveSpeed = source.MoveSpeed,
            AttackIntervalSeconds = source.AttackIntervalSeconds,
            SkillPower = source.SkillPower,
        };
    }
}
