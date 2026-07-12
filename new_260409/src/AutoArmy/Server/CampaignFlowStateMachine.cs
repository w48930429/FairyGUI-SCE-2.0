using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public sealed class CampaignFlowStateMachine
{
    public CampaignFlowState State { get; private set; } = CampaignFlowState.Idle;

    public int CurrentStageId { get; private set; } = 1;

    public int LastCompletedStageId { get; private set; }

    public BattleOutcome LastOutcome { get; private set; } = BattleOutcome.InProgress;

    public int LastRewardGold { get; private set; }

    public bool TryStartBattle(int stageId, int highestUnlockedStageId, out string? errorCode)
    {
        if (State != CampaignFlowState.Idle)
        {
            errorCode = CampaignErrorCodes.BattleAlreadyRunning;
            return false;
        }

        if (stageId < 1)
        {
            errorCode = CampaignErrorCodes.InvalidStage;
            return false;
        }

        if (stageId > Math.Max(1, highestUnlockedStageId))
        {
            errorCode = CampaignErrorCodes.StageLocked;
            return false;
        }

        CurrentStageId = stageId;
        State = CampaignFlowState.InBattle;
        errorCode = null;
        return true;
    }

    public void MarkBattleFinished(int stageId, BattleOutcome outcome, int rewardGold)
    {
        LastCompletedStageId = Math.Max(1, stageId);
        LastOutcome = outcome;
        LastRewardGold = Math.Max(0, rewardGold);
        State = CampaignFlowState.ResultPendingConfirm;
    }

    public bool TryConfirmResult(out string? errorCode)
    {
        if (State != CampaignFlowState.ResultPendingConfirm)
        {
            errorCode = CampaignErrorCodes.InvalidState;
            return false;
        }

        State = CampaignFlowState.Idle;
        errorCode = null;
        return true;
    }

    public bool CanUpgrade(out string? errorCode)
    {
        if (State != CampaignFlowState.Idle)
        {
            errorCode = CampaignErrorCodes.InvalidState;
            return false;
        }

        errorCode = null;
        return true;
    }
}
