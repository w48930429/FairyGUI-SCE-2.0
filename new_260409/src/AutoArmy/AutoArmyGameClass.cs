#if CLIENT
using GameEntry.AutoArmy.Client;
#endif
using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;
using TriggerEncapsulation.Messaging;

namespace GameEntry;

public class AutoArmyGameClass : IGameClass
{
    private const int CampaignInitialGold = 180;
    private const int HeroConfigId = 101;
    private const float ServerTickDeltaTimeSeconds = 0.1f;
    private const int ServerTickMilliseconds = 100;
    private const int SnapshotPublishIntervalMilliseconds = 250;

    private static readonly IPlayerProgressRepository CampaignRepository = CreateCampaignRepository();
    private static readonly CampaignService CampaignService = new(CampaignRepository);
    private static readonly string[] AvailableServerIds = ["S1", "S2", "S3"];

#if SERVER
    private static readonly object CampaignRuntimeGate = new();
    private static readonly Dictionary<string, PlayerCampaignRuntime> CampaignRuntimes = new(StringComparer.Ordinal);
    private static readonly ISessionRouter<Player> SessionRouter = new InMemorySessionRouter<Player>();
    private static readonly UserIdentityMigrationService IdentityMigration = new(CampaignRepository, CampaignInitialGold);
    private static bool _serverMessageHandlersRegistered;
#endif

#if CLIENT
    private static bool _clientMessageHandlerRegistered;
    private static BattleCanvasView? _battleCanvasView;
#endif

    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
#if CLIENT
        Game.OnGameUIInitialization += OnGameUiInitialization;
        Game.OnGameStart += OnClientGameStart;
#endif
    }

    private static void OnGameTriggerInitialization()
    {
        if (!IsAutoArmyGameModeActive())
        {
            return;
        }

        TypedMessageHandler.Initialize();

#if SERVER
        RegisterServerMessageHandlers();
        Game.Logger.LogInformation("AutoArmy trigger initialization on server.");
#elif CLIENT
        EnsureClientMessageHandlers();
        Game.Logger.LogInformation("AutoArmy trigger initialization on client.");
#endif
    }

    private static bool IsAutoArmyGameModeActive()
    {
        if (Game.GameModeLink == ScopeData.GameDataGameMode.MapGameMode)
        {
            return true;
        }

        return Game.GameModeLink == GameCore.ScopeData.GameMode.Default &&
               GameDataGlobalConfig.TestGameMode == ScopeData.GameDataGameMode.MapGameMode;
    }

#if CLIENT
    private static void OnGameUiInitialization()
    {
        // Migration mode: disable AutoArmy project's built-in client overlay UI.
        // Keep game-side logic and messaging intact.
        Game.Logger.LogInformation("AutoArmy client UI initialization skipped on {Source}.", nameof(OnGameUiInitialization));
    }

    private static void OnClientGameStart()
    {
        // Migration mode: disable AutoArmy project's built-in client overlay UI.
        Game.Logger.LogInformation("AutoArmy client UI initialization skipped on {Source}.", nameof(OnClientGameStart));
    }

    private static void EnsureClientUiInitialized(string source)
    {
        if (!IsAutoArmyGameModeActive())
        {
            Game.Logger.LogInformation(
                "AutoArmy skipped client UI initialization on {Source}. current={CurrentMode}, expected={ExpectedMode}, test={TestMode}",
                source,
                Game.GameModeLink,
                ScopeData.GameDataGameMode.MapGameMode,
                GameDataGlobalConfig.TestGameMode);
            return;
        }

        EnsureClientMessageHandlers();
        _battleCanvasView ??= new BattleCanvasView(
            OnServerSelectedByUi,
            OnStartStageByUi,
            OnUpgradeHeroByUi,
            OnUpgradeTroopByUi,
            OnConfirmResultByUi);
        RequestServerSelectionState();
        RequestCampaignProgressState();
        Game.Logger.LogInformation("AutoArmy UI initialization on client from {Source}.", source);
    }

    private static void EnsureClientMessageHandlers()
    {
        if (_clientMessageHandlerRegistered)
        {
            return;
        }

        TypedMessageHandler.Initialize();
        TypedMessageHandler.Register<BattleSnapshot>(
            OnBattleSnapshot,
            MessagePriority.High,
            "auto-army-battle-snapshot");
        TypedMessageHandler.Register<ServerSelectionStateMessage>(
            OnServerSelectionState,
            MessagePriority.High,
            "auto-army-server-selection-state");
        TypedMessageHandler.Register<CampaignProgressStateMessage>(
            OnCampaignProgressState,
            MessagePriority.High,
            "auto-army-campaign-progress-state");
        TypedMessageHandler.Register<StartStageResultMessage>(
            OnStartStageResult,
            MessagePriority.High,
            "auto-army-start-stage-result");
        TypedMessageHandler.Register<OperationResultMessage>(
            OnOperationResult,
            MessagePriority.High,
            "auto-army-operation-result");
        _clientMessageHandlerRegistered = true;
    }

    private static Task<bool> OnBattleSnapshot(Player? sender, BattleSnapshot snapshot)
    {
        _ = sender;
        BattleCanvasView.UpdateLatestSnapshot(snapshot);
        return Task.FromResult(true);
    }

    private static Task<bool> OnServerSelectionState(Player? sender, ServerSelectionStateMessage message)
    {
        _ = sender;
        BattleCanvasView.UpdateServerSelectionState(message);
        return Task.FromResult(true);
    }

    private static Task<bool> OnCampaignProgressState(Player? sender, CampaignProgressStateMessage message)
    {
        _ = sender;
        BattleCanvasView.UpdateCampaignProgressState(message);
        return Task.FromResult(true);
    }

    private static Task<bool> OnStartStageResult(Player? sender, StartStageResultMessage message)
    {
        _ = sender;
        BattleCanvasView.UpdateStartStageResult(message);
        return Task.FromResult(true);
    }

    private static Task<bool> OnOperationResult(Player? sender, OperationResultMessage message)
    {
        _ = sender;
        BattleCanvasView.UpdateOperationResult(message);
        return Task.FromResult(true);
    }

    private static void OnServerSelectedByUi(string serverId)
    {
        var request = new ServerSelectionRequestMessage
        {
            ServerId = serverId,
        };

        _ = new TypedMessage<ServerSelectionRequestMessage>(request).SendToServer();
    }

    private static void OnStartStageByUi(int stageId)
    {
        var request = new StartStageRequestMessage
        {
            StageId = stageId,
        };

        _ = new TypedMessage<StartStageRequestMessage>(request).SendToServer();
    }

    private static void OnUpgradeHeroByUi()
    {
        var request = new UpgradeHeroRequestMessage
        {
            HeroConfigId = HeroConfigId,
        };

        _ = new TypedMessage<UpgradeHeroRequestMessage>(request).SendToServer();
    }

    private static void OnUpgradeTroopByUi(BattleUnitRole troopRole)
    {
        var request = new UpgradeTroopRequestMessage
        {
            TroopRole = troopRole,
        };

        _ = new TypedMessage<UpgradeTroopRequestMessage>(request).SendToServer();
    }

    private static void OnConfirmResultByUi()
    {
        _ = new TypedMessage<ConfirmBattleResultMessage>(new ConfirmBattleResultMessage()).SendToServer();
    }

    private static void RequestServerSelectionState()
    {
        _ = new TypedMessage<ServerSelectionQueryMessage>(new ServerSelectionQueryMessage()).SendToServer();
    }

    private static void RequestCampaignProgressState()
    {
        _ = new TypedMessage<CampaignProgressQueryMessage>(new CampaignProgressQueryMessage()).SendToServer();
    }
#endif

#if SERVER
    private static void RegisterServerMessageHandlers()
    {
        if (_serverMessageHandlersRegistered)
        {
            return;
        }

        TypedMessageHandler.Register<ServerSelectionRequestMessage>(
            OnServerSelectionRequested,
            MessagePriority.High,
            "auto-army-server-selection-request");
        TypedMessageHandler.Register<ServerSelectionQueryMessage>(
            OnServerSelectionQuery,
            MessagePriority.High,
            "auto-army-server-selection-query");
        TypedMessageHandler.Register<CampaignProgressQueryMessage>(
            OnCampaignProgressQuery,
            MessagePriority.High,
            "auto-army-campaign-progress-query");
        TypedMessageHandler.Register<StartStageRequestMessage>(
            OnStartStageRequest,
            MessagePriority.High,
            "auto-army-start-stage-request");
        TypedMessageHandler.Register<ConfirmBattleResultMessage>(
            OnConfirmBattleResultRequest,
            MessagePriority.High,
            "auto-army-confirm-battle-result-request");
        TypedMessageHandler.Register<UpgradeHeroRequestMessage>(
            OnUpgradeHeroRequest,
            MessagePriority.High,
            "auto-army-upgrade-hero-request");
        TypedMessageHandler.Register<UpgradeTroopRequestMessage>(
            OnUpgradeTroopRequest,
            MessagePriority.High,
            "auto-army-upgrade-troop-request");
        _serverMessageHandlersRegistered = true;
    }

    private static Task<bool> OnServerSelectionRequested(Player? sender, ServerSelectionRequestMessage message)
    {
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "select_server", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        if (message is null || string.IsNullOrWhiteSpace(message.ServerId))
        {
            return Task.FromResult(false);
        }

        var normalizedServerId = message.ServerId.Trim().ToUpperInvariant();
        if (!IsKnownServerId(normalizedServerId))
        {
            SendOperationResultTo(
                sender,
                "select_server",
                false,
                CampaignErrorCodes.InvalidStage,
                $"Invalid server id: {message.ServerId}");
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        lock (CampaignRuntimeGate)
        {
            runtime.SelectedServerId = normalizedServerId;
        }

        EnsureLegacyProgressMigrated(playerIdentity, normalizedServerId, sender.Id);
        SessionRouter.Bind(playerIdentity, normalizedServerId, sender);
        Game.Logger.LogInformation("AutoArmy selected server changed to {ServerId}", normalizedServerId);
        SendServerSelectionStateTo(playerIdentity, normalizedServerId);
        SendCampaignProgressStateTo(playerIdentity);
        return Task.FromResult(true);
    }

    private static Task<bool> OnServerSelectionQuery(Player? sender, ServerSelectionQueryMessage message)
    {
        _ = message;
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "query_server_selection", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        EnsureLegacyProgressMigrated(playerIdentity, runtime.SelectedServerId, sender.Id);
        SessionRouter.Bind(playerIdentity, runtime.SelectedServerId, sender);
        SendServerSelectionStateTo(playerIdentity, runtime.SelectedServerId);
        SendCampaignProgressStateTo(playerIdentity);
        return Task.FromResult(true);
    }

    private static Task<bool> OnCampaignProgressQuery(Player? sender, CampaignProgressQueryMessage message)
    {
        _ = message;
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "query_progress", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        EnsureLegacyProgressMigrated(playerIdentity, runtime.SelectedServerId, sender.Id);
        SessionRouter.Bind(playerIdentity, runtime.SelectedServerId, sender);
        SendCampaignProgressStateTo(playerIdentity);
        return Task.FromResult(true);
    }

    private static Task<bool> OnStartStageRequest(Player? sender, StartStageRequestMessage message)
    {
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "start_stage", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        if (message is null || message.StageId < 1)
        {
            SendOperationResultTo(sender, "start_stage", false, CampaignErrorCodes.InvalidStage, "Invalid stage id.");
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        var serverId = runtime.SelectedServerId;
        EnsureLegacyProgressMigrated(playerIdentity, serverId, sender.Id);
        SessionRouter.Bind(playerIdentity, serverId, sender);
        var playerId = BuildPlayerId(playerIdentity, serverId);
        var progressSummary = CampaignService.GetProgressSummary(playerId);

        string? errorCode;
        lock (CampaignRuntimeGate)
        {
            if (!runtime.Flow.TryStartBattle(message.StageId, progressSummary.HighestUnlockedStageId, out errorCode))
            {
                SendOperationResultTo(
                    sender,
                    "start_stage",
                    false,
                    errorCode ?? CampaignErrorCodes.InvalidState,
                    "Cannot start stage.");
                SendCampaignProgressStateTo(playerIdentity);
                return Task.FromResult(false);
            }
        }

        SendOperationResultTo(sender, "start_stage", true, string.Empty, $"Stage {message.StageId} started.");
        SendCampaignProgressStateTo(playerIdentity);
        _ = RunServerBattleLoop(playerIdentity, serverId, message.StageId);
        return Task.FromResult(true);
    }

    private static Task<bool> OnConfirmBattleResultRequest(Player? sender, ConfirmBattleResultMessage message)
    {
        _ = message;
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "confirm_result", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        SessionRouter.Bind(playerIdentity, runtime.SelectedServerId, sender);

        string? errorCode;
        lock (CampaignRuntimeGate)
        {
            if (!runtime.Flow.TryConfirmResult(out errorCode))
            {
                SendOperationResultTo(
                    sender,
                    "confirm_result",
                    false,
                    errorCode ?? CampaignErrorCodes.InvalidState,
                    "No pending result to confirm.");
                return Task.FromResult(false);
            }
        }

        SendOperationResultTo(sender, "confirm_result", true, string.Empty, "Result confirmed.");
        SendCampaignProgressStateTo(playerIdentity);
        return Task.FromResult(true);
    }

    private static Task<bool> OnUpgradeHeroRequest(Player? sender, UpgradeHeroRequestMessage message)
    {
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "upgrade_hero", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        if (message is null || message.HeroConfigId <= 0)
        {
            SendOperationResultTo(sender, "upgrade_hero", false, CampaignErrorCodes.InvalidStage, "Invalid hero id.");
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        EnsureLegacyProgressMigrated(playerIdentity, runtime.SelectedServerId, sender.Id);
        SessionRouter.Bind(playerIdentity, runtime.SelectedServerId, sender);

        string? flowError;
        lock (CampaignRuntimeGate)
        {
            if (!runtime.Flow.CanUpgrade(out flowError))
            {
                SendOperationResultTo(
                    sender,
                    "upgrade_hero",
                    false,
                    flowError ?? CampaignErrorCodes.InvalidState,
                    "Upgrade is not allowed during battle.");
                return Task.FromResult(false);
            }
        }

        var playerId = BuildPlayerId(playerIdentity, runtime.SelectedServerId);
        var success = CampaignService.TryUpgradeHero(playerId, message.HeroConfigId, out _);
        if (!success)
        {
            SendOperationResultTo(sender, "upgrade_hero", false, CampaignErrorCodes.InsufficientGold, "Gold is not enough for hero upgrade.");
            SendCampaignProgressStateTo(playerIdentity);
            return Task.FromResult(false);
        }

        SendOperationResultTo(sender, "upgrade_hero", true, string.Empty, "Hero upgraded.");
        SendCampaignProgressStateTo(playerIdentity);
        return Task.FromResult(true);
    }

    private static Task<bool> OnUpgradeTroopRequest(Player? sender, UpgradeTroopRequestMessage message)
    {
        if (sender is null)
        {
            return Task.FromResult(false);
        }

        if (!TryBuildUserIdentity(sender, out var playerIdentity, out var identityErrorCode, out var identityErrorMessage))
        {
            SendOperationResultTo(sender, "upgrade_troop", false, identityErrorCode, identityErrorMessage);
            return Task.FromResult(false);
        }

        if (message is null)
        {
            SendOperationResultTo(sender, "upgrade_troop", false, CampaignErrorCodes.InvalidStage, "Invalid troop role.");
            return Task.FromResult(false);
        }

        var runtime = GetOrCreateRuntime(playerIdentity);
        EnsureLegacyProgressMigrated(playerIdentity, runtime.SelectedServerId, sender.Id);
        SessionRouter.Bind(playerIdentity, runtime.SelectedServerId, sender);

        string? flowError;
        lock (CampaignRuntimeGate)
        {
            if (!runtime.Flow.CanUpgrade(out flowError))
            {
                SendOperationResultTo(
                    sender,
                    "upgrade_troop",
                    false,
                    flowError ?? CampaignErrorCodes.InvalidState,
                    "Upgrade is not allowed during battle.");
                return Task.FromResult(false);
            }
        }

        var playerId = BuildPlayerId(playerIdentity, runtime.SelectedServerId);
        var success = CampaignService.TryUpgradeTroop(playerId, message.TroopRole, out _);
        if (!success)
        {
            SendOperationResultTo(sender, "upgrade_troop", false, CampaignErrorCodes.InsufficientGold, "Gold is not enough for troop upgrade.");
            SendCampaignProgressStateTo(playerIdentity);
            return Task.FromResult(false);
        }

        SendOperationResultTo(sender, "upgrade_troop", true, string.Empty, $"{message.TroopRole} upgraded.");
        SendCampaignProgressStateTo(playerIdentity);
        return Task.FromResult(true);
    }

    private static async Task RunServerBattleLoop(string playerIdentity, string serverId, int stageId)
    {
        var playerId = BuildPlayerId(playerIdentity, serverId);
        var runtime = GetOrCreateRuntime(playerIdentity);
        BattleSession session;

        try
        {
            session = CampaignService.CreateBattleSession(playerId, stageId);
        }
        catch (KeyNotFoundException)
        {
            lock (CampaignRuntimeGate)
            {
                runtime.Flow.MarkBattleFinished(stageId, BattleOutcome.Defeat, rewardGold: 0);
            }

            SendOperationResultTo(playerIdentity, serverId, "start_stage", false, CampaignErrorCodes.InvalidStage, $"Stage {stageId} is not configured.");
            SendCampaignProgressStateTo(playerIdentity);
            return;
        }

        SendSnapshotTo(playerIdentity, serverId, session.LatestSnapshot);

        var publishAccumulator = 0;
        while (!session.LatestSnapshot.IsFinished)
        {
            var snapshot = session.Tick(ServerTickDeltaTimeSeconds);
            publishAccumulator += ServerTickMilliseconds;
            if (publishAccumulator >= SnapshotPublishIntervalMilliseconds || snapshot.IsFinished)
            {
                SendSnapshotTo(playerIdentity, serverId, snapshot);
                publishAccumulator = 0;
            }

            await Game.Delay(ServerTickMilliseconds);
        }

        var finalSnapshot = session.LatestSnapshot;
        var rewardGold = 0;
        if (finalSnapshot.Outcome == BattleOutcome.Victory &&
            CampaignService.TryCompleteStage(playerId, stageId, finalSnapshot.Outcome, out _))
        {
            rewardGold = CampaignService.GetStageDefinition(stageId).RewardGold;
        }

        lock (CampaignRuntimeGate)
        {
            runtime.Flow.MarkBattleFinished(stageId, finalSnapshot.Outcome, rewardGold);
        }

        var progressSummary = CampaignService.GetProgressSummary(playerId);
        SendSnapshotTo(playerIdentity, serverId, finalSnapshot);
        SendStartStageResultTo(playerIdentity, serverId, new StartStageResultMessage
        {
            StageId = stageId,
            Outcome = finalSnapshot.Outcome,
            RewardGold = rewardGold,
            HighestUnlockedStageId = progressSummary.HighestUnlockedStageId,
        });

        Game.Logger.LogInformation(
            "AutoArmy battle finished. stage={StageId}, outcome={Outcome}, rewardGold={RewardGold}, unlocked={Unlocked}, gold={Gold}",
            stageId,
            finalSnapshot.Outcome,
            rewardGold,
            progressSummary.HighestUnlockedStageId,
            progressSummary.Gold);

        SendCampaignProgressStateTo(playerIdentity);
    }

    private static void SendSnapshotTo(string playerIdentity, string serverId, BattleSnapshot snapshot)
    {
        if (TryResolveRecipient(playerIdentity, serverId, out var recipient))
        {
            _ = new TypedMessage<BattleSnapshot>(snapshot).SendTo(recipient);
        }
    }

    private static void SendServerSelectionStateTo(string playerIdentity, string serverId)
    {
        var state = new ServerSelectionStateMessage
        {
            SelectedServerId = serverId,
            AvailableServerIds = [.. AvailableServerIds],
        };

        if (TryResolveRecipient(playerIdentity, serverId, out var recipient))
        {
            _ = new TypedMessage<ServerSelectionStateMessage>(state).SendTo(recipient);
        }
    }

    private static void SendCampaignProgressStateTo(string playerIdentity)
    {
        var runtime = GetOrCreateRuntime(playerIdentity);
        var serverId = runtime.SelectedServerId;
        var playerId = BuildPlayerId(playerIdentity, serverId);
        var progressSummary = CampaignService.GetProgressSummary(playerId);
        var stageSummaries = CampaignService.GetStageSummaries(playerId);

        CampaignFlowState flowState;
        int currentStageId;
        int lastCompletedStageId;
        BattleOutcome lastOutcome;
        int lastRewardGold;
        lock (CampaignRuntimeGate)
        {
            flowState = runtime.Flow.State;
            currentStageId = runtime.Flow.CurrentStageId;
            lastCompletedStageId = runtime.Flow.LastCompletedStageId;
            lastOutcome = runtime.Flow.LastOutcome;
            lastRewardGold = runtime.Flow.LastRewardGold;
        }

        var message = new CampaignProgressStateMessage
        {
            ServerId = serverId,
            FlowState = flowState,
            CurrentStageId = Math.Max(1, currentStageId),
            Gold = progressSummary.Gold,
            HighestUnlockedStageId = progressSummary.HighestUnlockedStageId,
            HeroLevel = progressSummary.HeroLevel,
            GuardLevel = progressSummary.GuardLevel,
            RangerLevel = progressSummary.RangerLevel,
            HeroUpgradeCost = progressSummary.HeroUpgradeCost,
            GuardUpgradeCost = progressSummary.GuardUpgradeCost,
            RangerUpgradeCost = progressSummary.RangerUpgradeCost,
            LastCompletedStageId = lastCompletedStageId,
            LastOutcome = lastOutcome,
            LastRewardGold = lastRewardGold,
            Stages = stageSummaries
                .Select(summary => new CampaignStageStateMessage
                {
                    StageId = summary.StageId,
                    NodeId = summary.NodeId,
                    RecommendedPower = summary.RecommendedPower,
                    RewardGold = summary.RewardGold,
                    IsUnlocked = summary.IsUnlocked,
                })
                .ToArray(),
        };

        if (TryResolveRecipient(playerIdentity, serverId, out var recipient))
        {
            _ = new TypedMessage<CampaignProgressStateMessage>(message).SendTo(recipient);
        }
    }

    private static void SendStartStageResultTo(string playerIdentity, string serverId, StartStageResultMessage message)
    {
        if (TryResolveRecipient(playerIdentity, serverId, out var recipient))
        {
            _ = new TypedMessage<StartStageResultMessage>(message).SendTo(recipient);
        }
    }

    private static void SendOperationResultTo(
        Player recipient,
        string operation,
        bool success,
        string errorCode,
        string message)
    {
        _ = new TypedMessage<OperationResultMessage>(new OperationResultMessage
        {
            Operation = operation,
            Success = success,
            ErrorCode = errorCode,
            Message = message,
        }).SendTo(recipient);
    }

    private static void SendOperationResultTo(
        string playerIdentity,
        string serverId,
        string operation,
        bool success,
        string errorCode,
        string message)
    {
        if (TryResolveRecipient(playerIdentity, serverId, out var recipient))
        {
            SendOperationResultTo(recipient, operation, success, errorCode, message);
        }
    }

    private static string BuildPlayerId(string playerIdentity, string serverId)
    {
        return $"{playerIdentity}@{serverId}";
    }

    private static bool IsKnownServerId(string serverId)
    {
        foreach (var availableServerId in AvailableServerIds)
        {
            if (string.Equals(availableServerId, serverId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveRecipient(string playerIdentity, string serverId, out Player recipient)
    {
        if (SessionRouter.TryResolve(playerIdentity, serverId, out var route) && route is not null)
        {
            recipient = route.Recipient;
            return true;
        }

        recipient = null!;
        return false;
    }

    private static bool TryBuildUserIdentity(
        Player? sender,
        out string playerIdentity,
        out string errorCode,
        out string errorMessage)
    {
        if (sender?.UserId is long userId && userId > 0)
        {
            playerIdentity = $"u:{userId}";
            errorCode = string.Empty;
            errorMessage = string.Empty;
            return true;
        }

        playerIdentity = string.Empty;
        errorCode = CampaignErrorCodes.UnauthenticatedUser;
        errorMessage = "User id is required for campaign progress.";
        return false;
    }

    private static void EnsureLegacyProgressMigrated(string playerIdentity, string serverId, int fallbackPlayerSlotId)
    {
        var migrated = IdentityMigration.EnsureMigrated(playerIdentity, serverId, fallbackPlayerSlotId);
        if (migrated)
        {
            Game.Logger.LogInformation(
                "AutoArmy migrated legacy progress to user identity. user={UserIdentity}, server={ServerId}, fallbackSlot={SlotId}",
                playerIdentity,
                serverId,
                fallbackPlayerSlotId);
        }
    }

    private static PlayerCampaignRuntime GetOrCreateRuntime(string playerIdentity)
    {
        lock (CampaignRuntimeGate)
        {
            if (CampaignRuntimes.TryGetValue(playerIdentity, out var runtime))
            {
                return runtime;
            }

            runtime = new PlayerCampaignRuntime();
            CampaignRuntimes[playerIdentity] = runtime;
            return runtime;
        }
    }
#endif

    private static IPlayerProgressRepository CreateCampaignRepository()
    {
#if SERVER
        var storagePath = Path.Combine("project", "autoarmy", "campaign-progress.json");
        return new FileBackedPlayerProgressRepository(storagePath, initialGold: CampaignInitialGold);
#else
        return new InMemoryPlayerProgressRepository(initialGold: CampaignInitialGold);
#endif
    }

#if SERVER
    private sealed class PlayerCampaignRuntime
    {
        public string SelectedServerId { get; set; } = "S1";

        public CampaignFlowStateMachine Flow { get; } = new();
    }
#endif
}
