using Xunit;
using GameEntry.AutoArmy.Shared;

namespace AutoArmy.Shared.Tests;

public class BattleBalanceTests
{
    [Fact]
    public void RoleAdvantageTable_ReturnsConfiguredMultiplier()
    {
        var table = RoleAdvantageTable.CreateDefault();

        var multiplier = table.GetDamageMultiplier(BattleUnitRole.Guard, BattleUnitRole.Striker);

        Assert.Equal(1.25f, multiplier, 3);
    }

    [Fact]
    public void RoleAdvantageTable_ReturnsNeutralMultiplierForMissingPair()
    {
        var table = new RoleAdvantageTable();

        var multiplier = table.GetDamageMultiplier(BattleUnitRole.Caster, BattleUnitRole.Caster);

        Assert.Equal(1f, multiplier, 3);
    }

    [Fact]
    public void BattleDamageFormula_AppliesAttackDefenseAndRoleMultiplier()
    {
        var damage = BattleDamageFormula.CalculateDamage(
            attackerRole: BattleUnitRole.Guard,
            defenderRole: BattleUnitRole.Striker,
            baseDamage: 10f,
            attack: 5f,
            defense: 3f,
            roleMultiplier: 1.25f);

        Assert.Equal(15f, damage, 3);
    }

    [Fact]
    public void BattleDamageFormula_HasMinimumDamageFloor()
    {
        var damage = BattleDamageFormula.CalculateDamage(
            attackerRole: BattleUnitRole.Ranger,
            defenderRole: BattleUnitRole.Guard,
            baseDamage: 1f,
            attack: 1f,
            defense: 99f,
            roleMultiplier: 0.5f);

        Assert.Equal(1f, damage, 3);
    }
}
