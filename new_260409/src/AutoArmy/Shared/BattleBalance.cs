namespace GameEntry.AutoArmy.Shared;

public static class BattleConstants
{
    public const int NoUnitId = -1;
}

public static class BattleBalance
{
    public const float NeutralRoleMultiplier = 1f;
    public const float MinimumDamage = 1f;
}

public sealed class RoleAdvantageTable
{
    private readonly Dictionary<RolePair, float> _multipliers = new();

    public RoleAdvantageTable SetMultiplier(BattleUnitRole attackerRole, BattleUnitRole defenderRole, float multiplier)
    {
        _multipliers[new RolePair(attackerRole, defenderRole)] = multiplier;
        return this;
    }

    public float GetDamageMultiplier(BattleUnitRole attackerRole, BattleUnitRole defenderRole)
    {
        return _multipliers.TryGetValue(new RolePair(attackerRole, defenderRole), out var multiplier)
            ? multiplier
            : BattleBalance.NeutralRoleMultiplier;
    }

    public static RoleAdvantageTable CreateDefault()
    {
        return new RoleAdvantageTable()
            .SetMultiplier(BattleUnitRole.Guard, BattleUnitRole.Striker, 1.25f)
            .SetMultiplier(BattleUnitRole.Striker, BattleUnitRole.Ranger, 1.25f)
            .SetMultiplier(BattleUnitRole.Ranger, BattleUnitRole.Caster, 1.25f)
            .SetMultiplier(BattleUnitRole.Caster, BattleUnitRole.Guard, 1.25f)
            .SetMultiplier(BattleUnitRole.Striker, BattleUnitRole.Guard, 0.9f)
            .SetMultiplier(BattleUnitRole.Ranger, BattleUnitRole.Striker, 0.9f)
            .SetMultiplier(BattleUnitRole.Caster, BattleUnitRole.Ranger, 0.9f)
            .SetMultiplier(BattleUnitRole.Guard, BattleUnitRole.Caster, 0.9f);
    }

    private readonly record struct RolePair(BattleUnitRole AttackerRole, BattleUnitRole DefenderRole);
}

public static class BattleDamageFormula
{
    public static float CalculateDamage(
        BattleUnitRole attackerRole,
        BattleUnitRole defenderRole,
        float baseDamage,
        float attack,
        float defense,
        float roleMultiplier)
    {
        _ = attackerRole;
        _ = defenderRole;

        var reducedDamage = MathF.Max(0f, baseDamage) + MathF.Max(0f, attack) - MathF.Max(0f, defense);
        var scaledDamage = reducedDamage * MathF.Max(0f, roleMultiplier);
        return MathF.Max(BattleBalance.MinimumDamage, scaledDamage);
    }

    public static float CalculateDamage(
        BattleUnitRole attackerRole,
        BattleUnitRole defenderRole,
        float baseDamage,
        float attack,
        float defense,
        RoleAdvantageTable roleAdvantageTable)
    {
        var roleMultiplier = roleAdvantageTable.GetDamageMultiplier(attackerRole, defenderRole);
        return CalculateDamage(attackerRole, defenderRole, baseDamage, attack, defense, roleMultiplier);
    }
}
