namespace GameEntry.AutoArmy.Shared;

public enum CampaignFlowState
{
    Idle = 0,
    InBattle = 1,
    ResultPendingConfirm = 2,
}

public static class CampaignErrorCodes
{
    public const string InvalidStage = "invalid_stage";
    public const string StageLocked = "stage_locked";
    public const string BattleAlreadyRunning = "battle_already_running";
    public const string InsufficientGold = "insufficient_gold";
    public const string InvalidState = "invalid_state";
    public const string UnauthenticatedUser = "unauthenticated_user";
}
