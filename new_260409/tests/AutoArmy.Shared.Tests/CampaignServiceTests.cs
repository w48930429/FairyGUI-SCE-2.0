using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;
using Xunit;

namespace AutoArmy.Shared.Tests;

public class CampaignServiceTests
{
    [Fact]
    public void TryUpgradeHero_WhenGoldEnough_ConsumesGoldAndIncreasesLevel()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 200);
        var service = new CampaignService(repository);
        var playerId = "p-hero-upgrade";

        var before = service.GetProgress(playerId);

        var success = service.TryUpgradeHero(playerId, heroConfigId: 101, out var after);

        Assert.True(success);
        Assert.Equal(before.GetHeroLevel(101) + 1, after.GetHeroLevel(101));
        Assert.True(after.Gold < before.Gold);
    }

    [Fact]
    public void TryUpgradeTroop_WhenGoldNotEnough_ReturnsFalseWithoutLevelIncrease()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 0);
        var service = new CampaignService(repository);
        var playerId = "p-troop-upgrade-fail";

        var success = service.TryUpgradeTroop(playerId, BattleUnitRole.Guard, out var after);

        Assert.False(success);
        Assert.Equal(1, after.GetTroopLevel(BattleUnitRole.Guard));
        Assert.Equal(0, after.Gold);
    }

    [Fact]
    public void TryCompleteStage_OnVictory_AwardsGoldAndUnlocksNextLinearStage()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 100, initialUnlockedStageId: 1);
        var service = new CampaignService(repository);
        var playerId = "p-stage-complete";

        var before = service.GetProgress(playerId);
        var success = service.TryCompleteStage(playerId, stageId: 1, BattleOutcome.Victory, out var after);

        Assert.True(success);
        Assert.Equal(2, after.HighestUnlockedStageId);
        Assert.True(after.Gold > before.Gold);
    }

    [Fact]
    public void BuildFixedAllyFormation_AppliesProgressLevelsToNextBattleStats()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 1000);
        var service = new CampaignService(repository);
        var playerId = "p-next-battle-growth";

        var baseFormation = service.BuildFixedAllyFormation(playerId);
        var baseHero = Assert.Single(baseFormation, static unit => unit.ConfigId == 101);
        var baseRanger = Assert.Single(baseFormation, static unit => unit.ConfigId == 103);

        Assert.True(service.TryUpgradeHero(playerId, heroConfigId: 101, out _));
        Assert.True(service.TryUpgradeTroop(playerId, BattleUnitRole.Ranger, out _));

        var upgradedFormation = service.BuildFixedAllyFormation(playerId);
        var upgradedHero = Assert.Single(upgradedFormation, static unit => unit.ConfigId == 101);
        var upgradedRanger = Assert.Single(upgradedFormation, static unit => unit.ConfigId == 103);

        Assert.Equal(baseHero.Level + 1, upgradedHero.Level);
        Assert.True(upgradedHero.Stats.Attack > baseHero.Stats.Attack);
        Assert.Equal(baseRanger.Level + 1, upgradedRanger.Level);
        Assert.True(upgradedRanger.Stats.Attack > baseRanger.Stats.Attack);
    }

    [Fact]
    public void GetStageSummaries_ContainsUnlockStateForPlayerProgress()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 120, initialUnlockedStageId: 2);
        var service = new CampaignService(repository);

        var summaries = service.GetStageSummaries("p-stage-summary");

        Assert.NotEmpty(summaries);
        var stage1 = Assert.Single(summaries, static stage => stage.StageId == 1);
        var stage2 = Assert.Single(summaries, static stage => stage.StageId == 2);
        var stage3 = Assert.Single(summaries, static stage => stage.StageId == 3);
        Assert.True(stage1.IsUnlocked);
        Assert.True(stage2.IsUnlocked);
        Assert.False(stage3.IsUnlocked);
    }

    [Fact]
    public void GetProgressSummary_ReturnsGoldLevelsAndUpgradeCosts()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 200);
        var service = new CampaignService(repository);
        var playerId = "p-progress-summary";

        var summary = service.GetProgressSummary(playerId);

        Assert.Equal(200, summary.Gold);
        Assert.Equal(1, summary.HeroLevel);
        Assert.Equal(1, summary.GuardLevel);
        Assert.Equal(1, summary.RangerLevel);
        Assert.Equal(CampaignService.GetHeroUpgradeCost(1), summary.HeroUpgradeCost);
        Assert.Equal(CampaignService.GetTroopUpgradeCost(1), summary.GuardUpgradeCost);
        Assert.Equal(CampaignService.GetTroopUpgradeCost(1), summary.RangerUpgradeCost);
    }
}
