using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public sealed class BattleSession
{
    private BattleSession(BattleWorld world)
    {
        World = world;
        LatestSnapshot = world.CreateSnapshot();
    }

    public BattleWorld World { get; }

    public BattleSnapshot LatestSnapshot { get; private set; }

    public BattleSnapshot Tick(float deltaTimeSeconds)
    {
        LatestSnapshot = BattleSystems.Tick(World, deltaTimeSeconds);
        return LatestSnapshot;
    }

    public BattleSnapshot RunToCompletion(float deltaTimeSeconds, int maxTicks)
    {
        for (var tick = 0; tick < maxTicks && !LatestSnapshot.IsFinished; tick++)
        {
            Tick(deltaTimeSeconds);
        }

        if (!LatestSnapshot.IsFinished)
        {
            World.Outcome = BattleOutcome.Draw;
            LatestSnapshot = World.CreateSnapshot();
        }

        return LatestSnapshot;
    }

    public static BattleSession CreateFixedDebugSession()
    {
        var world = new BattleWorld();
        foreach (var definition in CreateFixedDebugUnits())
        {
            world.SpawnUnit(definition);
        }

        return new BattleSession(world);
    }

    public static BattleSnapshot RunFixedDebugBattle(float deltaTimeSeconds = 0.25f, int maxTicks = 240)
    {
        return CreateFixedDebugSession().RunToCompletion(deltaTimeSeconds, maxTicks);
    }

    public static BattleSession CreateSession(
        IEnumerable<BattleUnitDefinition> unitDefinitions,
        RoleAdvantageTable? roleAdvantageTable = null,
        int stageId = 1)
    {
        ArgumentNullException.ThrowIfNull(unitDefinitions);

        var world = new BattleWorld(roleAdvantageTable);
        world.StageId = Math.Max(1, stageId);
        foreach (var definition in unitDefinitions)
        {
            world.SpawnUnit(definition);
        }

        return new BattleSession(world);
    }

    private static IReadOnlyList<BattleUnitDefinition> CreateFixedDebugUnits()
    {
        return
        [
            new BattleUnitDefinition
            {
                ConfigId = 101,
                Team = BattleTeam.Ally,
                Role = BattleUnitRole.Caster,
                Kind = BattleUnitKind.Hero,
                Level = 3,
                PositionY = 10f,
                MaxHealth = 44f,
                Stats = new BattleStatsComponent
                {
                    Attack = 4f,
                    Defense = 2f,
                    AttackRange = 3.5f,
                    MoveSpeed = 1.5f,
                    AttackIntervalSeconds = 1.4f,
                    SkillPower = 8f,
                },
                PassiveSkill = new PassiveSkillComponent
                {
                    SkillId = BattleSystems.RangerAuraSkillId,
                    Potency = 1f,
                },
                AutoCastSkill = new AutoCastSkillComponent
                {
                    SkillId = BattleSystems.FireballSkillId,
                    Potency = 1f,
                    CooldownSeconds = 4f,
                    CastRange = 4.5f,
                    TargetRule = BattleSkillTargetRule.CurrentTarget,
                },
                InitialSkillCooldownSeconds = 1f,
            },
            new BattleUnitDefinition
            {
                ConfigId = 102,
                Team = BattleTeam.Ally,
                Role = BattleUnitRole.Guard,
                Kind = BattleUnitKind.Soldier,
                Level = 2,
                PositionY = 11.5f,
                MaxHealth = 60f,
                Stats = new BattleStatsComponent
                {
                    Attack = 6f,
                    Defense = 4f,
                    AttackRange = 1.4f,
                    MoveSpeed = 1.1f,
                    AttackIntervalSeconds = 1f,
                    SkillPower = 0f,
                },
            },
            new BattleUnitDefinition
            {
                ConfigId = 103,
                Team = BattleTeam.Ally,
                Role = BattleUnitRole.Ranger,
                Kind = BattleUnitKind.Soldier,
                Level = 2,
                PositionY = 13f,
                MaxHealth = 38f,
                Stats = new BattleStatsComponent
                {
                    Attack = 9f,
                    Defense = 1.5f,
                    AttackRange = 5.5f,
                    MoveSpeed = 1.4f,
                    AttackIntervalSeconds = 1.2f,
                    SkillPower = 0f,
                },
            },
            new BattleUnitDefinition
            {
                ConfigId = 201,
                Team = BattleTeam.Enemy,
                Role = BattleUnitRole.Guard,
                Kind = BattleUnitKind.Soldier,
                Level = 2,
                PositionY = 0f,
                MaxHealth = 56f,
                Stats = new BattleStatsComponent
                {
                    Attack = 6f,
                    Defense = 3.5f,
                    AttackRange = 1.4f,
                    MoveSpeed = 1.1f,
                    AttackIntervalSeconds = 1f,
                    SkillPower = 0f,
                },
            },
            new BattleUnitDefinition
            {
                ConfigId = 202,
                Team = BattleTeam.Enemy,
                Role = BattleUnitRole.Striker,
                Kind = BattleUnitKind.Soldier,
                Level = 2,
                PositionY = -1.5f,
                MaxHealth = 46f,
                Stats = new BattleStatsComponent
                {
                    Attack = 10f,
                    Defense = 2f,
                    AttackRange = 1.3f,
                    MoveSpeed = 1.8f,
                    AttackIntervalSeconds = 0.9f,
                    SkillPower = 0f,
                },
            },
            new BattleUnitDefinition
            {
                ConfigId = 203,
                Team = BattleTeam.Enemy,
                Role = BattleUnitRole.Caster,
                Kind = BattleUnitKind.Soldier,
                Level = 2,
                PositionY = 1.5f,
                MaxHealth = 36f,
                Stats = new BattleStatsComponent
                {
                    Attack = 4f,
                    Defense = 1.5f,
                    AttackRange = 3.8f,
                    MoveSpeed = 1.2f,
                    AttackIntervalSeconds = 1.5f,
                    SkillPower = 6f,
                },
                AutoCastSkill = new AutoCastSkillComponent
                {
                    SkillId = BattleSystems.FireballSkillId,
                    Potency = 1f,
                    CooldownSeconds = 5f,
                    CastRange = 4f,
                    TargetRule = BattleSkillTargetRule.CurrentTarget,
                },
                InitialSkillCooldownSeconds = 2f,
            },
        ];
    }
}
