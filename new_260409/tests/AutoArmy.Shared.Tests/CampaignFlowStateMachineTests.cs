using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;
using Xunit;

namespace AutoArmy.Shared.Tests;

public class CampaignFlowStateMachineTests
{
    [Fact]
    public void StartStage_WhenIdleAndUnlocked_TransitionsToInBattle()
    {
        var machine = new CampaignFlowStateMachine();

        var started = machine.TryStartBattle(stageId: 2, highestUnlockedStageId: 2, out var errorCode);

        Assert.True(started);
        Assert.Null(errorCode);
        Assert.Equal(CampaignFlowState.InBattle, machine.State);
        Assert.Equal(2, machine.CurrentStageId);
    }

    [Fact]
    public void StartStage_WhenStageLocked_ReturnsStageLockedError()
    {
        var machine = new CampaignFlowStateMachine();

        var started = machine.TryStartBattle(stageId: 3, highestUnlockedStageId: 2, out var errorCode);

        Assert.False(started);
        Assert.Equal(CampaignErrorCodes.StageLocked, errorCode);
        Assert.Equal(CampaignFlowState.Idle, machine.State);
    }

    [Fact]
    public void ConfirmResult_WhenResultPending_TransitionsBackToIdle()
    {
        var machine = new CampaignFlowStateMachine();
        Assert.True(machine.TryStartBattle(stageId: 1, highestUnlockedStageId: 1, out _));

        machine.MarkBattleFinished(stageId: 1, BattleOutcome.Victory, rewardGold: 60);

        var confirmed = machine.TryConfirmResult(out var errorCode);

        Assert.True(confirmed);
        Assert.Null(errorCode);
        Assert.Equal(CampaignFlowState.Idle, machine.State);
        Assert.Equal(BattleOutcome.Victory, machine.LastOutcome);
        Assert.Equal(60, machine.LastRewardGold);
    }

    [Fact]
    public void Upgrade_WhenInBattle_ReturnsInvalidState()
    {
        var machine = new CampaignFlowStateMachine();
        Assert.True(machine.TryStartBattle(stageId: 1, highestUnlockedStageId: 1, out _));

        var allowed = machine.CanUpgrade(out var errorCode);

        Assert.False(allowed);
        Assert.Equal(CampaignErrorCodes.InvalidState, errorCode);
    }
}
