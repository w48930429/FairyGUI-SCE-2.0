using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;
using Xunit;

namespace AutoArmy.Shared.Tests;

public class BattlePathfindingTests
{
    [Fact]
    public void UpdateMovement_WhenForwardBlockedByAlly_PerformsSidestep()
    {
        var world = new BattleWorld();

        _ = world.SpawnUnit(new BattleUnitDefinition
        {
            ConfigId = 1001,
            Team = BattleTeam.Ally,
            Role = BattleUnitRole.Guard,
            Kind = BattleUnitKind.Soldier,
            PositionX = 0f,
            PositionY = 10f,
            MaxHealth = 50f,
            Stats = new BattleStatsComponent
            {
                Attack = 5f,
                Defense = 4f,
                AttackRange = 1.2f,
                MoveSpeed = 0f,
                AttackIntervalSeconds = 1f,
                SkillPower = 0f,
            },
        });

        var moverId = world.SpawnUnit(new BattleUnitDefinition
        {
            ConfigId = 1002,
            Team = BattleTeam.Ally,
            Role = BattleUnitRole.Striker,
            Kind = BattleUnitKind.Soldier,
            PositionX = 0f,
            PositionY = 12f,
            MaxHealth = 44f,
            Stats = new BattleStatsComponent
            {
                Attack = 7f,
                Defense = 2f,
                AttackRange = 1.2f,
                MoveSpeed = 2f,
                AttackIntervalSeconds = 1f,
                SkillPower = 0f,
            },
        });

        _ = world.SpawnUnit(new BattleUnitDefinition
        {
            ConfigId = 2001,
            Team = BattleTeam.Enemy,
            Role = BattleUnitRole.Guard,
            Kind = BattleUnitKind.Soldier,
            PositionX = 0f,
            PositionY = 0f,
            MaxHealth = 60f,
            Stats = new BattleStatsComponent
            {
                Attack = 4f,
                Defense = 3f,
                AttackRange = 1.2f,
                MoveSpeed = 0f,
                AttackIntervalSeconds = 1f,
                SkillPower = 0f,
            },
        });

        _ = BattleSystems.Tick(world, 0.5f);

        Assert.NotEqual(0f, world.Transforms[moverId].PositionX);
    }

    [Fact]
    public void AcquireTargets_Uses2dDistanceForNearestEnemySelection()
    {
        var world = new BattleWorld();

        var allyId = world.SpawnUnit(new BattleUnitDefinition
        {
            ConfigId = 1101,
            Team = BattleTeam.Ally,
            Role = BattleUnitRole.Ranger,
            Kind = BattleUnitKind.Soldier,
            PositionX = 0f,
            PositionY = 8f,
            MaxHealth = 40f,
            TargetSearchRadius = 20f,
            Stats = new BattleStatsComponent
            {
                Attack = 6f,
                Defense = 1.5f,
                AttackRange = 4f,
                MoveSpeed = 0f,
                AttackIntervalSeconds = 1f,
                SkillPower = 0f,
            },
        });

        var nearIn2dId = world.SpawnUnit(new BattleUnitDefinition
        {
            ConfigId = 2101,
            Team = BattleTeam.Enemy,
            Role = BattleUnitRole.Guard,
            Kind = BattleUnitKind.Soldier,
            PositionX = 0f,
            PositionY = 3f,
            MaxHealth = 55f,
            Stats = new BattleStatsComponent
            {
                Attack = 5f,
                Defense = 3f,
                AttackRange = 1.2f,
                MoveSpeed = 0f,
                AttackIntervalSeconds = 1f,
                SkillPower = 0f,
            },
        });

        _ = world.SpawnUnit(new BattleUnitDefinition
        {
            ConfigId = 2102,
            Team = BattleTeam.Enemy,
            Role = BattleUnitRole.Striker,
            Kind = BattleUnitKind.Soldier,
            PositionX = 6f,
            PositionY = 4.5f,
            MaxHealth = 48f,
            Stats = new BattleStatsComponent
            {
                Attack = 8f,
                Defense = 2f,
                AttackRange = 1.2f,
                MoveSpeed = 0f,
                AttackIntervalSeconds = 1f,
                SkillPower = 0f,
            },
        });

        _ = BattleSystems.Tick(world, 0.1f);

        Assert.Equal(nearIn2dId, world.Targeting[allyId].CurrentTargetUnitId);
    }
}
