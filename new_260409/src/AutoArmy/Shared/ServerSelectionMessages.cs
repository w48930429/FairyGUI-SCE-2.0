namespace GameEntry.AutoArmy.Shared;

public sealed class ServerSelectionRequestMessage
{
    public string ServerId { get; set; } = string.Empty;
}

public sealed class ServerSelectionQueryMessage
{
}

public sealed class ServerSelectionStateMessage
{
    public string SelectedServerId { get; set; } = string.Empty;

    public string[] AvailableServerIds { get; set; } = [];
}

public sealed class CampaignProgressQueryMessage
{
}

public sealed class StartStageRequestMessage
{
    public int StageId { get; set; } = 1;
}

public sealed class ConfirmBattleResultMessage
{
}

public sealed class UpgradeHeroRequestMessage
{
    public int HeroConfigId { get; set; } = 101;
}

public sealed class UpgradeTroopRequestMessage
{
    public BattleUnitRole TroopRole { get; set; } = BattleUnitRole.Guard;
}

public sealed class CampaignStageStateMessage
{
    public int StageId { get; set; }

    public string NodeId { get; set; } = string.Empty;

    public int RecommendedPower { get; set; }

    public int RewardGold { get; set; }

    public bool IsUnlocked { get; set; }
}

public sealed class CampaignProgressStateMessage
{
    public string ServerId { get; set; } = string.Empty;

    public CampaignFlowState FlowState { get; set; } = CampaignFlowState.Idle;

    public int CurrentStageId { get; set; } = 1;

    public int Gold { get; set; }

    public int HighestUnlockedStageId { get; set; } = 1;

    public int HeroLevel { get; set; } = 1;

    public int GuardLevel { get; set; } = 1;

    public int RangerLevel { get; set; } = 1;

    public int HeroUpgradeCost { get; set; }

    public int GuardUpgradeCost { get; set; }

    public int RangerUpgradeCost { get; set; }

    public int LastCompletedStageId { get; set; }

    public BattleOutcome LastOutcome { get; set; } = BattleOutcome.InProgress;

    public int LastRewardGold { get; set; }

    public CampaignStageStateMessage[] Stages { get; set; } = [];
}

public sealed class StartStageResultMessage
{
    public int StageId { get; set; } = 1;

    public BattleOutcome Outcome { get; set; } = BattleOutcome.InProgress;

    public int RewardGold { get; set; }

    public int HighestUnlockedStageId { get; set; } = 1;
}

public sealed class OperationResultMessage
{
    public string Operation { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
