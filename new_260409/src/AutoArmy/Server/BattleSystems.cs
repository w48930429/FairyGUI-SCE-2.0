using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public static class BattleSystems
{
    public const string RangerAuraSkillId = "hero.ranger-aura";
    public const string FireballSkillId = "hero.fireball";
    private const float LaneBoundaryX = 5.5f;
    private const float ForwardBlockDistance = 1.8f;
    private const float ForwardBlockWidth = 1.2f;

    public static BattleSnapshot Tick(BattleWorld world, float deltaTimeSeconds)
    {
        world.AdvanceTime(deltaTimeSeconds);
        world.ClearVisualEvents();

        CleanupTargets(world);
        RefreshStatsFromBase(world);
        ApplyPassives(world);
        AcquireTargets(world);
        UpdateMovement(world, deltaTimeSeconds);
        UpdateBasicAttacks(world, deltaTimeSeconds);
        UpdateAutoCastSkills(world, deltaTimeSeconds);
        CleanupDead(world);
        ResolveOutcome(world);

        return world.CreateSnapshot();
    }

    private static void CleanupTargets(BattleWorld world)
    {
        foreach (var unitId in world.Roles.Keys.OrderBy(static id => id))
        {
            if (!world.IsAlive(unitId))
            {
                continue;
            }

            var targetId = world.Targeting[unitId].CurrentTargetUnitId;
            if (targetId != BattleConstants.NoUnitId && !world.IsAlive(targetId))
            {
                world.Targeting[unitId].CurrentTargetUnitId = BattleConstants.NoUnitId;
                world.Attacks[unitId].CurrentTargetUnitId = BattleConstants.NoUnitId;
            }
        }
    }

    private static void RefreshStatsFromBase(BattleWorld world)
    {
        foreach (var unitId in world.Roles.Keys.OrderBy(static id => id))
        {
            if (!world.IsAlive(unitId))
            {
                continue;
            }

            var baseStats = world.BaseStats[unitId];
            var stats = world.Stats[unitId];
            stats.Attack = baseStats.Attack;
            stats.Defense = baseStats.Defense;
            stats.AttackRange = baseStats.AttackRange;
            stats.MoveSpeed = baseStats.MoveSpeed;
            stats.AttackIntervalSeconds = baseStats.AttackIntervalSeconds;
            stats.SkillPower = baseStats.SkillPower;

            world.Attacks[unitId].AttackIntervalSeconds = stats.AttackIntervalSeconds;
            world.Movement[unitId].StopDistance = stats.AttackRange;
        }
    }

    private static void ApplyPassives(BattleWorld world)
    {
        foreach (var unitId in world.PassiveSkills.Keys.OrderBy(static id => id))
        {
            if (!world.IsAlive(unitId))
            {
                continue;
            }

            var passive = world.PassiveSkills[unitId];
            if (!passive.IsEnabled || passive.SkillId != RangerAuraSkillId)
            {
                continue;
            }

            var team = world.Roles[unitId].Team;
            foreach (var allyId in world.GetAliveUnitIds(team))
            {
                if (world.Roles[allyId].Role != BattleUnitRole.Ranger)
                {
                    continue;
                }

                world.Stats[allyId].Attack += 2f * passive.Potency;
            }
        }
    }

    private static void AcquireTargets(BattleWorld world)
    {
        foreach (var unitId in world.GetAliveUnitIds())
        {
            if (world.Targeting[unitId].CurrentTargetUnitId != BattleConstants.NoUnitId)
            {
                continue;
            }

            var targetId = FindNearestEnemy(world, unitId, world.Targeting[unitId].SearchRadius);
            world.Targeting[unitId].CurrentTargetUnitId = targetId;
            world.Attacks[unitId].CurrentTargetUnitId = targetId;
        }
    }

    private static void UpdateMovement(BattleWorld world, float deltaTimeSeconds)
    {
        foreach (var unitId in world.GetAliveUnitIds())
        {
            var targeting = world.Targeting[unitId];
            var targetId = targeting.CurrentTargetUnitId;
            if (targetId == BattleConstants.NoUnitId || !world.IsAlive(targetId))
            {
                world.VisualStates[unitId].State = BattleVisualState.Idle;
                continue;
            }

            var transform = world.Transforms[unitId];
            var targetTransform = world.Transforms[targetId];
            var deltaY = targetTransform.PositionY - transform.PositionY;
            var directionY = MathF.Sign(deltaY);
            var distance = GetDistance(transform, targetTransform);
            var attackRange = world.Stats[unitId].AttackRange;
            if (distance <= attackRange)
            {
                world.VisualStates[unitId].State = BattleVisualState.Idle;
                continue;
            }

            var moveBudget = MathF.Min(world.Stats[unitId].MoveSpeed * deltaTimeSeconds, distance - attackRange);
            if (moveBudget <= 0f)
            {
                world.VisualStates[unitId].State = BattleVisualState.Idle;
                continue;
            }

            var blockerId = FindForwardAllyBlocker(world, unitId, directionY);
            if (blockerId != BattleConstants.NoUnitId)
            {
                var sidestepDirection = ComputeSidestepDirection(world, unitId, blockerId, targetId);
                var sidestepDistance = moveBudget * 0.75f;
                transform.PositionX = Math.Clamp(
                    transform.PositionX + sidestepDirection * sidestepDistance,
                    -LaneBoundaryX,
                    LaneBoundaryX);
                var forwardDistance = moveBudget * 0.25f;
                transform.PositionY += directionY * forwardDistance;
            }
            else
            {
                var preferredLaneX = GetPreferredLaneX(world.Roles[unitId].Role, world.Roles[unitId].Team);
                var laneDeltaX = preferredLaneX - transform.PositionX;
                var lateralMove = Math.Clamp(laneDeltaX, -moveBudget * 0.35f, moveBudget * 0.35f);
                transform.PositionX = Math.Clamp(transform.PositionX + lateralMove, -LaneBoundaryX, LaneBoundaryX);
                var forwardDistance = MathF.Max(0f, moveBudget - MathF.Abs(lateralMove));
                transform.PositionY += directionY * forwardDistance;
            }

            world.Movement[unitId].DirectionY = directionY;
            world.VisualStates[unitId].State = BattleVisualState.Moving;
        }
    }

    private static void UpdateBasicAttacks(BattleWorld world, float deltaTimeSeconds)
    {
        foreach (var unitId in world.GetAliveUnitIds())
        {
            var attack = world.Attacks[unitId];
            attack.CooldownRemainingSeconds = MathF.Max(0f, attack.CooldownRemainingSeconds - deltaTimeSeconds);

            var targetId = world.Targeting[unitId].CurrentTargetUnitId;
            if (targetId == BattleConstants.NoUnitId || !world.IsAlive(targetId))
            {
                continue;
            }

            if (!IsWithinRange(world, unitId, targetId, world.Stats[unitId].AttackRange))
            {
                continue;
            }

            if (attack.CooldownRemainingSeconds > 0f)
            {
                continue;
            }

            DealDamage(world, unitId, targetId, 0f, world.Stats[unitId].Attack, BattleVisualEventType.AttackHit);
            attack.CooldownRemainingSeconds = MathF.Max(0.25f, attack.AttackIntervalSeconds);
            world.VisualStates[unitId].State = BattleVisualState.Attacking;
        }
    }

    private static void UpdateAutoCastSkills(BattleWorld world, float deltaTimeSeconds)
    {
        foreach (var unitId in world.AutoCastSkills.Keys.OrderBy(static id => id))
        {
            if (!world.IsAlive(unitId))
            {
                continue;
            }

            var autoCast = world.AutoCastSkills[unitId];
            autoCast.CooldownRemainingSeconds = MathF.Max(0f, autoCast.CooldownRemainingSeconds - deltaTimeSeconds);
            if (autoCast.CooldownRemainingSeconds > 0f || autoCast.SkillId != FireballSkillId)
            {
                continue;
            }

            var targetId = SelectSkillTarget(world, unitId, autoCast);
            if (targetId == BattleConstants.NoUnitId || !IsWithinRange(world, unitId, targetId, autoCast.CastRange))
            {
                continue;
            }

            world.AddVisualEvent(BattleVisualEventType.SkillCast, unitId, targetId);
            DealDamage(world, unitId, targetId, 8f * autoCast.Potency, world.Stats[unitId].SkillPower, BattleVisualEventType.SkillHit);
            autoCast.CooldownRemainingSeconds = MathF.Max(1f, autoCast.CooldownSeconds);
            world.Targeting[unitId].CurrentTargetUnitId = targetId;
            world.Attacks[unitId].CurrentTargetUnitId = targetId;
            world.VisualStates[unitId].State = BattleVisualState.Casting;
        }
    }

    private static int SelectSkillTarget(BattleWorld world, int unitId, AutoCastSkillComponent autoCast)
    {
        var currentTargetId = world.Targeting[unitId].CurrentTargetUnitId;
        if (currentTargetId != BattleConstants.NoUnitId && world.IsAlive(currentTargetId))
        {
            return currentTargetId;
        }

        if (autoCast.TargetRule == BattleSkillTargetRule.LowestHealthEnemy)
        {
            return world.GetAliveUnitIds(GetEnemyTeam(world.Roles[unitId].Team))
                .OrderBy(id => world.Health[id].CurrentHealth)
                .FirstOrDefault(BattleConstants.NoUnitId);
        }

        return FindNearestEnemy(world, unitId, autoCast.CastRange);
    }

    private static int FindNearestEnemy(BattleWorld world, int unitId, float searchRadius)
    {
        var team = world.Roles[unitId].Team;
        var source = world.Transforms[unitId];

        return world.GetAliveUnitIds(GetEnemyTeam(team))
            .Select(targetId => new
            {
                TargetId = targetId,
                Distance = GetDistance(source, world.Transforms[targetId]),
            })
            .Where(candidate => candidate.Distance <= searchRadius)
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.TargetId)
            .Select(candidate => candidate.TargetId)
            .FirstOrDefault(BattleConstants.NoUnitId);
    }

    private static bool IsWithinRange(BattleWorld world, int sourceUnitId, int targetUnitId, float range)
    {
        var distance = GetDistance(world.Transforms[sourceUnitId], world.Transforms[targetUnitId]);
        return distance <= range;
    }

    private static float GetDistance(BattleTransform2DComponent source, BattleTransform2DComponent target)
    {
        var deltaX = target.PositionX - source.PositionX;
        var deltaY = target.PositionY - source.PositionY;
        return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static int FindForwardAllyBlocker(BattleWorld world, int unitId, float directionY)
    {
        var sourceTransform = world.Transforms[unitId];
        var team = world.Roles[unitId].Team;

        return world.GetAliveUnitIds(team)
            .Where(otherId => otherId != unitId)
            .Select(otherId => new
            {
                UnitId = otherId,
                DeltaY = world.Transforms[otherId].PositionY - sourceTransform.PositionY,
                DeltaX = world.Transforms[otherId].PositionX - sourceTransform.PositionX,
            })
            .Where(candidate =>
                MathF.Abs(candidate.DeltaX) <= ForwardBlockWidth &&
                MathF.Abs(candidate.DeltaY) <= ForwardBlockDistance &&
                MathF.Sign(candidate.DeltaY) == directionY)
            .OrderBy(candidate => MathF.Abs(candidate.DeltaY))
            .ThenBy(candidate => candidate.UnitId)
            .Select(candidate => candidate.UnitId)
            .FirstOrDefault(BattleConstants.NoUnitId);
    }

    private static float ComputeSidestepDirection(BattleWorld world, int unitId, int blockerId, int targetId)
    {
        var source = world.Transforms[unitId];
        var blocker = world.Transforms[blockerId];
        var target = world.Transforms[targetId];
        var awayFromBlocker = MathF.Sign(source.PositionX - blocker.PositionX);
        if (awayFromBlocker != 0f)
        {
            return awayFromBlocker;
        }

        var towardTarget = MathF.Sign(target.PositionX - source.PositionX);
        return towardTarget != 0f ? towardTarget : (unitId % 2 == 0 ? 1f : -1f);
    }

    private static float GetPreferredLaneX(BattleUnitRole role, BattleTeam team)
    {
        var baseOffset = role switch
        {
            BattleUnitRole.Guard => -1.2f,
            BattleUnitRole.Striker => -0.4f,
            BattleUnitRole.Ranger => 0.5f,
            BattleUnitRole.Caster => 1.1f,
            _ => 0f,
        };

        return team == BattleTeam.Ally ? baseOffset : -baseOffset;
    }

    private static void DealDamage(
        BattleWorld world,
        int attackerId,
        int targetId,
        float baseDamage,
        float attackPower,
        BattleVisualEventType damageEventType)
    {
        if (!world.IsAlive(attackerId) || !world.IsAlive(targetId))
        {
            return;
        }

        var attackerRole = world.Roles[attackerId].Role;
        var defenderRole = world.Roles[targetId].Role;
        var defense = world.Stats[targetId].Defense;
        var damage = BattleDamageFormula.CalculateDamage(
            attackerRole,
            defenderRole,
            baseDamage,
            attackPower,
            defense,
            world.RoleAdvantageTable);
        world.AddVisualEvent(damageEventType, attackerId, targetId, damage);

        var targetHealth = world.Health[targetId];
        targetHealth.CurrentHealth = MathF.Max(0f, targetHealth.CurrentHealth - damage);
        if (targetHealth.CurrentHealth <= 0f)
        {
            targetHealth.IsDead = true;
            world.VisualStates[targetId].State = BattleVisualState.Dead;
            world.Targeting[targetId].CurrentTargetUnitId = BattleConstants.NoUnitId;
            world.Attacks[targetId].CurrentTargetUnitId = BattleConstants.NoUnitId;
            world.AddVisualEvent(BattleVisualEventType.UnitDied, targetId);
            return;
        }

        world.VisualStates[targetId].State = BattleVisualState.Hurt;
    }

    private static void CleanupDead(BattleWorld world)
    {
        foreach (var unitId in world.Roles.Keys.OrderBy(static id => id))
        {
            if (!world.IsAlive(unitId))
            {
                world.VisualStates[unitId].State = BattleVisualState.Dead;
            }
        }
    }

    private static void ResolveOutcome(BattleWorld world)
    {
        var allyAlive = world.GetAliveCount(BattleTeam.Ally);
        var enemyAlive = world.GetAliveCount(BattleTeam.Enemy);

        world.Outcome = allyAlive == 0 && enemyAlive == 0
            ? BattleOutcome.Draw
            : enemyAlive == 0
                ? BattleOutcome.Victory
                : allyAlive == 0
                    ? BattleOutcome.Defeat
                    : BattleOutcome.InProgress;
    }

    private static BattleTeam GetEnemyTeam(BattleTeam team)
    {
        return team == BattleTeam.Ally ? BattleTeam.Enemy : BattleTeam.Ally;
    }
}
