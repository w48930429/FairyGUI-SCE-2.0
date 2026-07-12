#if CLIENT
using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Client;

public sealed class BattleCanvasView : IDisposable
{
    private const float StagePadding = 24f;
    private const string DefaultFontName = "AutoArmyDefaultFont";
    private static readonly object SnapshotGate = new();
    private static readonly object ServerSelectionGate = new();
    private static readonly object CampaignGate = new();
    private static readonly object FeedbackGate = new();
    private static readonly object FontGate = new();
    private static readonly string[] FontPathCandidates =
    [
        "ui/font/regular/RegularBold.otf",
        "ui/font/Regular/RegularBold.otf",
        "ui/font/regular/Regular.otf",
        "ui/font/Regular/Regular.otf",
    ];
    private static int _defaultFontId = int.MinValue;

    private static BattleSnapshot? _latestSnapshot;
    private static ServerSelectionStateMessage _serverSelectionState = new()
    {
        SelectedServerId = "S1",
        AvailableServerIds = ["S1", "S2", "S3"],
    };

    private static CampaignProgressStateMessage _campaignProgressState = new()
    {
        ServerId = "S1",
        FlowState = CampaignFlowState.Idle,
        CurrentStageId = 1,
        Gold = 0,
        HighestUnlockedStageId = 1,
        HeroLevel = 1,
        GuardLevel = 1,
        RangerLevel = 1,
        HeroUpgradeCost = 40,
        GuardUpgradeCost = 30,
        RangerUpgradeCost = 30,
        Stages =
        [
            new CampaignStageStateMessage
            {
                StageId = 1,
                NodeId = "1-1",
                RecommendedPower = 100,
                RewardGold = 60,
                IsUnlocked = true,
            },
        ],
    };

    private static StartStageResultMessage? _latestStageResult;
    private static string _latestOperationText = string.Empty;
    private static DateTime _latestOperationExpireAtUtc = DateTime.MinValue;

    private readonly CanvasAnimated _canvas;
    private readonly Action<string> _onServerSelected;
    private readonly Action<int> _onStartStage;
    private readonly Action _onUpgradeHero;
    private readonly Action<BattleUnitRole> _onUpgradeTroop;
    private readonly Action _onConfirmResult;
    private readonly List<ServerSelectorHitbox> _selectorHitboxes = [];
    private readonly List<ActionHitbox> _actionHitboxes = [];
    private readonly List<ServerButtonEntry> _serverButtons = [];
    private readonly List<StageButtonEntry> _stageButtons = [];
    private string[] _serverButtonSignature = [];
    private int[] _stageButtonSignature = [];

    private Panel? _uiRoot;
    private Panel? _serverPanel;
    private Label? _serverTitleLabel;
    private Label? _headerTitleLabel;
    private Label? _headerDetailLabel;
    private Label? _headerStatusLabel;
    private Panel? _campaignPanel;
    private Label? _campaignProgressLabel;
    private Label? _campaignFlowLabel;
    private Label? _campaignResultLabel;
    private ButtonWithLabel _startStageButton;
    private ButtonWithLabel _upgradeHeroButton;
    private ButtonWithLabel _upgradeGuardButton;
    private ButtonWithLabel _upgradeRangerButton;
    private ButtonWithLabel _confirmResultButton;
    private Panel? _feedbackPanel;
    private Label? _feedbackLabel;

    private int _selectedStageId = 1;

    public BattleCanvasView(
        Action<string> onServerSelected,
        Action<int> onStartStage,
        Action onUpgradeHero,
        Action<BattleUnitRole> onUpgradeTroop,
        Action onConfirmResult)
    {
        _onServerSelected = onServerSelected ?? throw new ArgumentNullException(nameof(onServerSelected));
        _onStartStage = onStartStage ?? throw new ArgumentNullException(nameof(onStartStage));
        _onUpgradeHero = onUpgradeHero ?? throw new ArgumentNullException(nameof(onUpgradeHero));
        _onUpgradeTroop = onUpgradeTroop ?? throw new ArgumentNullException(nameof(onUpgradeTroop));
        _onConfirmResult = onConfirmResult ?? throw new ArgumentNullException(nameof(onConfirmResult));
        _canvas = new CanvasAnimated();
        _canvas.FullScreen();
        _canvas.StartTiming();
        _canvas.OnAnimatedRender += OnAnimatedRender;
        _canvas.OnPointerClicked += OnPointerClicked;
        _canvas.AddToVisualTree();
        EnsureDefaultFontLoaded();
        InitializeControlOverlay();
    }

    public static void UpdateLatestSnapshot(BattleSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (SnapshotGate)
        {
            _latestSnapshot = snapshot;
        }
    }

    public static void UpdateServerSelectionState(ServerSelectionStateMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var normalizedIds = message.AvailableServerIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            normalizedIds = ["S1", "S2", "S3"];
        }

        var normalizedSelected = string.IsNullOrWhiteSpace(message.SelectedServerId)
            ? normalizedIds[0]
            : message.SelectedServerId.Trim().ToUpperInvariant();

        if (!normalizedIds.Contains(normalizedSelected, StringComparer.Ordinal))
        {
            normalizedSelected = normalizedIds[0];
        }

        lock (ServerSelectionGate)
        {
            _serverSelectionState = new ServerSelectionStateMessage
            {
                SelectedServerId = normalizedSelected,
                AvailableServerIds = normalizedIds,
            };
        }
    }

    public static void UpdateCampaignProgressState(CampaignProgressStateMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (CampaignGate)
        {
            _campaignProgressState = new CampaignProgressStateMessage
            {
                ServerId = message.ServerId,
                FlowState = message.FlowState,
                CurrentStageId = Math.Max(1, message.CurrentStageId),
                Gold = message.Gold,
                HighestUnlockedStageId = Math.Max(1, message.HighestUnlockedStageId),
                HeroLevel = Math.Max(1, message.HeroLevel),
                GuardLevel = Math.Max(1, message.GuardLevel),
                RangerLevel = Math.Max(1, message.RangerLevel),
                HeroUpgradeCost = Math.Max(0, message.HeroUpgradeCost),
                GuardUpgradeCost = Math.Max(0, message.GuardUpgradeCost),
                RangerUpgradeCost = Math.Max(0, message.RangerUpgradeCost),
                LastCompletedStageId = Math.Max(0, message.LastCompletedStageId),
                LastOutcome = message.LastOutcome,
                LastRewardGold = Math.Max(0, message.LastRewardGold),
                Stages = message.Stages
                    .OrderBy(static stage => stage.StageId)
                    .Select(static stage => new CampaignStageStateMessage
                    {
                        StageId = Math.Max(1, stage.StageId),
                        NodeId = stage.NodeId,
                        RecommendedPower = Math.Max(0, stage.RecommendedPower),
                        RewardGold = Math.Max(0, stage.RewardGold),
                        IsUnlocked = stage.IsUnlocked,
                    })
                    .ToArray(),
            };
        }
    }

    public static void UpdateStartStageResult(StartStageResultMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (FeedbackGate)
        {
            _latestStageResult = new StartStageResultMessage
            {
                StageId = Math.Max(1, message.StageId),
                Outcome = message.Outcome,
                RewardGold = Math.Max(0, message.RewardGold),
                HighestUnlockedStageId = Math.Max(1, message.HighestUnlockedStageId),
            };

            var resultText = message.Outcome == BattleOutcome.Victory
                ? $"Stage {message.StageId} cleared, +{Math.Max(0, message.RewardGold)} gold."
                : $"Stage {message.StageId} failed.";
            _latestOperationText = resultText;
            _latestOperationExpireAtUtc = DateTime.UtcNow.AddSeconds(4);
        }
    }

    public static void UpdateOperationResult(OperationResultMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (FeedbackGate)
        {
            var status = message.Success ? "OK" : "FAIL";
            var suffix = string.IsNullOrWhiteSpace(message.ErrorCode) ? string.Empty : $" ({message.ErrorCode})";
            _latestOperationText = $"[{status}] {message.Operation}: {message.Message}{suffix}";
            _latestOperationExpireAtUtc = DateTime.UtcNow.AddSeconds(4);
        }
    }

    public void Dispose()
    {
        _canvas.OnAnimatedRender -= OnAnimatedRender;
        _canvas.OnPointerClicked -= OnPointerClicked;
        _canvas.Dispose();
        _uiRoot?.Dispose();
        _uiRoot = null;
    }

    private static BattleSnapshot? GetLatestSnapshot()
    {
        lock (SnapshotGate)
        {
            return _latestSnapshot;
        }
    }

    private static ServerSelectionStateMessage GetServerSelectionState()
    {
        lock (ServerSelectionGate)
        {
            return new ServerSelectionStateMessage
            {
                SelectedServerId = _serverSelectionState.SelectedServerId,
                AvailableServerIds = [.. _serverSelectionState.AvailableServerIds],
            };
        }
    }

    private static CampaignProgressStateMessage GetCampaignProgressState()
    {
        lock (CampaignGate)
        {
            return new CampaignProgressStateMessage
            {
                ServerId = _campaignProgressState.ServerId,
                FlowState = _campaignProgressState.FlowState,
                CurrentStageId = _campaignProgressState.CurrentStageId,
                Gold = _campaignProgressState.Gold,
                HighestUnlockedStageId = _campaignProgressState.HighestUnlockedStageId,
                HeroLevel = _campaignProgressState.HeroLevel,
                GuardLevel = _campaignProgressState.GuardLevel,
                RangerLevel = _campaignProgressState.RangerLevel,
                HeroUpgradeCost = _campaignProgressState.HeroUpgradeCost,
                GuardUpgradeCost = _campaignProgressState.GuardUpgradeCost,
                RangerUpgradeCost = _campaignProgressState.RangerUpgradeCost,
                LastCompletedStageId = _campaignProgressState.LastCompletedStageId,
                LastOutcome = _campaignProgressState.LastOutcome,
                LastRewardGold = _campaignProgressState.LastRewardGold,
                Stages = _campaignProgressState.Stages
                    .Select(static stage => new CampaignStageStateMessage
                    {
                        StageId = stage.StageId,
                        NodeId = stage.NodeId,
                        RecommendedPower = stage.RecommendedPower,
                        RewardGold = stage.RewardGold,
                        IsUnlocked = stage.IsUnlocked,
                    })
                    .ToArray(),
            };
        }
    }

    private static string? GetLatestOperationText()
    {
        lock (FeedbackGate)
        {
            if (DateTime.UtcNow > _latestOperationExpireAtUtc)
            {
                return null;
            }

            return _latestOperationText;
        }
    }

    private void InitializeControlOverlay()
    {
        _uiRoot = new Panel
        {
            Width = 0f,
            Height = 0f,
            WidthStretchRatio = 1f,
            HeightStretchRatio = 1f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
        };
        _ = _uiRoot.AddToVisualTree();

        _headerTitleLabel = new Label
        {
            Parent = _uiRoot,
            Text = "AUTO ARMY CAMPAIGN",
            FontSize = 20f,
            Bold = true,
            TextColor = Color.FromArgb(240, 245, 246, 250),
            Width = 420f,
            Height = 24f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Left,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(StagePadding, StagePadding + 8f, 0f, 0f),
            IsStatic = true,
        };

        _headerDetailLabel = new Label
        {
            Parent = _uiRoot,
            Text = "Waiting for server snapshot...",
            FontSize = 14f,
            TextColor = Color.FromArgb(220, 217, 224, 232),
            Width = 620f,
            Height = 22f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Left,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(StagePadding, StagePadding + 34f, 0f, 0f),
            IsStatic = true,
        };

        _headerStatusLabel = new Label
        {
            Parent = _uiRoot,
            Text = string.Empty,
            FontSize = 14f,
            TextColor = Color.FromArgb(220, 217, 224, 232),
            Width = 520f,
            Height = 22f,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Right,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(0f, StagePadding + 34f, StagePadding, 0f),
            IsStatic = true,
        };

        _serverPanel = new Panel
        {
            Parent = _uiRoot,
            Width = 238f,
            Height = 74f,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0f, StagePadding + 2f, StagePadding, 0f),
            Background = new SolidColorBrush(Color.FromArgb(170, 14, 22, 34)),
            CornerRadius = 10f,
        };

        _serverTitleLabel = new Label
        {
            Parent = _serverPanel,
            Text = "Server",
            FontSize = 12f,
            TextColor = Color.FromArgb(210, 229, 236, 245),
            Width = 90f,
            Height = 18f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Left,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(10f, 6f, 0f, 0f),
            IsStatic = true,
        };

        _campaignPanel = new Panel
        {
            Parent = _uiRoot,
            Width = 360f,
            Height = 280f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(StagePadding, 0f, 0f, StagePadding),
            Background = new SolidColorBrush(Color.FromArgb(210, 14, 22, 34)),
            CornerRadius = 12f,
        };

        _campaignProgressLabel = new Label
        {
            Parent = _campaignPanel,
            Text = "Progress · Gold 0",
            FontSize = 16f,
            TextColor = Color.FromArgb(240, 237, 242, 249),
            Width = 336f,
            Height = 22f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Left,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(12f, 12f, 0f, 0f),
            IsStatic = true,
        };

        _campaignFlowLabel = new Label
        {
            Parent = _campaignPanel,
            Text = "Flow Idle · Unlocked Stage 1",
            FontSize = 12f,
            TextColor = Color.FromArgb(220, 197, 214, 236),
            Width = 336f,
            Height = 20f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Left,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(12f, 34f, 0f, 0f),
            IsStatic = true,
        };

        _startStageButton = CreateButtonWithLabel(_campaignPanel, 12f, 132f, 336f, 28f, "Start Stage 1");
        _startStageButton.Button.OnPointerClicked += (sender, e) =>
        {
            _ = sender;
            _ = e;
            _onStartStage(Math.Max(1, _selectedStageId));
        };

        _upgradeHeroButton = CreateButtonWithLabel(_campaignPanel, 12f, 168f, 336f, 24f, "Hero Lv.1 Cost 40");
        _upgradeHeroButton.Button.OnPointerClicked += (sender, e) =>
        {
            _ = sender;
            _ = e;
            _onUpgradeHero();
        };

        _upgradeGuardButton = CreateButtonWithLabel(_campaignPanel, 12f, 196f, 336f, 24f, "Guard Lv.1 Cost 30");
        _upgradeGuardButton.Button.OnPointerClicked += (sender, e) =>
        {
            _ = sender;
            _ = e;
            _onUpgradeTroop(BattleUnitRole.Guard);
        };

        _upgradeRangerButton = CreateButtonWithLabel(_campaignPanel, 12f, 224f, 336f, 24f, "Ranger Lv.1 Cost 30");
        _upgradeRangerButton.Button.OnPointerClicked += (sender, e) =>
        {
            _ = sender;
            _ = e;
            _onUpgradeTroop(BattleUnitRole.Ranger);
        };

        _campaignResultLabel = new Label
        {
            Parent = _campaignPanel,
            Text = string.Empty,
            FontSize = 12f,
            TextColor = Color.FromArgb(230, 241, 224, 178),
            Width = 210f,
            Height = 20f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Left,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            Margin = new Thickness(12f, 252f, 0f, 0f),
            IsStatic = true,
            Visible = false,
        };

        _confirmResultButton = CreateButtonWithLabel(_campaignPanel, 236f, 246f, 112f, 24f, "Confirm");
        _confirmResultButton.Button.OnPointerClicked += (sender, e) =>
        {
            _ = sender;
            _ = e;
            _onConfirmResult();
        };
        _confirmResultButton.Button.Visible = false;

        _feedbackPanel = new Panel
        {
            Parent = _uiRoot,
            Width = 620f,
            Height = 30f,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0f, 0f, 0f, 8f),
            Background = new SolidColorBrush(Color.FromArgb(180, 9, 16, 24)),
            CornerRadius = 9f,
            Visible = false,
        };

        _feedbackLabel = new Label
        {
            Parent = _feedbackPanel,
            Text = string.Empty,
            FontSize = 12f,
            TextColor = Color.FromArgb(240, 232, 241, 252),
            Width = 620f,
            Height = 30f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Center,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            IsStatic = true,
        };
    }

    private void UpdateControlOverlay(BattleSnapshot? snapshot, CampaignProgressStateMessage campaignState)
    {
        if (_uiRoot is null ||
            _serverPanel is null ||
            _headerTitleLabel is null ||
            _headerDetailLabel is null ||
            _headerStatusLabel is null ||
            _campaignProgressLabel is null ||
            _campaignFlowLabel is null ||
            _campaignResultLabel is null ||
            _feedbackPanel is null ||
            _feedbackLabel is null)
        {
            return;
        }

        EnsureSelectedStage(campaignState);

        if (snapshot is null)
        {
            _headerDetailLabel.Text = "Waiting for server snapshot...";
            _headerStatusLabel.Text = "Status: Waiting";
        }
        else
        {
            _headerDetailLabel.Text = $"Elapsed {snapshot.ElapsedSeconds:0.00}s · Ally {snapshot.AllyAliveCount} vs Enemy {snapshot.EnemyAliveCount}";
            _headerStatusLabel.Text = snapshot.Status == BattleStatus.Running
                ? "Status: Running"
                : $"Status: Finished · Winner: {snapshot.WinnerTeam?.ToString() ?? "None"}";
        }

        var serverState = GetServerSelectionState();
        var serverIds = serverState.AvailableServerIds.Length > 0 ? serverState.AvailableServerIds : ["S1", "S2", "S3"];
        EnsureServerButtons(serverIds);
        UpdateServerButtons(serverState.SelectedServerId);

        _campaignProgressLabel.Text = $"Progress · Gold {campaignState.Gold}";
        _campaignFlowLabel.Text = $"Flow {campaignState.FlowState} · Unlocked Stage {campaignState.HighestUnlockedStageId}";

        var stages = campaignState.Stages.Length == 0
            ? [new CampaignStageStateMessage { StageId = 1, NodeId = "1-1", IsUnlocked = true }]
            : campaignState.Stages;
        EnsureStageButtons(stages);
        UpdateStageButtons(stages);

        var canAct = campaignState.FlowState == CampaignFlowState.Idle;
        var selectedStageUnlocked = stages.Any(stage => stage.StageId == _selectedStageId && stage.IsUnlocked);

        _startStageButton.Label.Text = $"Start Stage {_selectedStageId}";
        SetActionButtonState(_startStageButton, canAct && selectedStageUnlocked, highlight: true);

        _upgradeHeroButton.Label.Text = $"Hero Lv.{campaignState.HeroLevel} Cost {campaignState.HeroUpgradeCost}";
        _upgradeGuardButton.Label.Text = $"Guard Lv.{campaignState.GuardLevel} Cost {campaignState.GuardUpgradeCost}";
        _upgradeRangerButton.Label.Text = $"Ranger Lv.{campaignState.RangerLevel} Cost {campaignState.RangerUpgradeCost}";
        SetActionButtonState(_upgradeHeroButton, canAct, highlight: false);
        SetActionButtonState(_upgradeGuardButton, canAct, highlight: false);
        SetActionButtonState(_upgradeRangerButton, canAct, highlight: false);

        var hasPendingResult = campaignState.FlowState == CampaignFlowState.ResultPendingConfirm;
        _campaignResultLabel.Visible = hasPendingResult;
        _confirmResultButton.Button.Visible = hasPendingResult;
        _confirmResultButton.Label.Visible = hasPendingResult;
        if (hasPendingResult)
        {
            _campaignResultLabel.Text = campaignState.LastOutcome == BattleOutcome.Victory
                ? $"Result: Victory +{campaignState.LastRewardGold} Gold"
                : $"Result: {campaignState.LastOutcome}";
        }

        SetActionButtonState(_confirmResultButton, hasPendingResult, highlight: false);

        var feedback = GetLatestOperationText();
        _feedbackPanel.Visible = !string.IsNullOrWhiteSpace(feedback);
        _feedbackLabel.Text = feedback ?? string.Empty;
    }

    private static ButtonWithLabel CreateButtonWithLabel(GameUI.Control.Control parent, float left, float top, float width, float height, string text)
    {
        var button = new Button
        {
            Parent = parent,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(left, top, 0f, 0f),
            CornerRadius = 7f,
            Background = new SolidColorBrush(Color.FromArgb(220, 36, 118, 196)),
        };

        var label = new Label
        {
            Parent = button,
            Text = text,
            FontSize = 12f,
            TextColor = Color.FromArgb(245, 247, 250, 255),
            Width = 0f,
            Height = 0f,
            WidthStretchRatio = 1f,
            HeightStretchRatio = 1f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalContentAlignment.Center,
            VerticalContentAlignment = VerticalContentAlignment.Center,
            IsStatic = true,
        };

        return new ButtonWithLabel(button, label);
    }

    private void EnsureServerButtons(string[] serverIds)
    {
        if (_serverPanel is null)
        {
            return;
        }

        if (_serverButtonSignature.SequenceEqual(serverIds, StringComparer.Ordinal))
        {
            return;
        }

        foreach (var entry in _serverButtons)
        {
            entry.Button.Dispose();
        }

        _serverButtons.Clear();
        _serverButtonSignature = [.. serverIds];

        var chipWidth = 62f;
        var chipHeight = 30f;
        var chipGap = 8f;
        var startX = 10f;
        var topY = 24f;

        for (var index = 0; index < serverIds.Length; index++)
        {
            var serverId = serverIds[index];
            var left = startX + index * (chipWidth + chipGap);
            var buttonPair = CreateButtonWithLabel(_serverPanel, left, topY, chipWidth, chipHeight, serverId);

            buttonPair.Button.OnPointerClicked += (sender, e) =>
            {
                _ = sender;
                _ = e;
                _onServerSelected(serverId);
            };

            _serverButtons.Add(new ServerButtonEntry(serverId, buttonPair.Button, buttonPair.Label));
        }
    }

    private void UpdateServerButtons(string selectedServerId)
    {
        foreach (var entry in _serverButtons)
        {
            var selected = string.Equals(entry.ServerId, selectedServerId, StringComparison.Ordinal);
            entry.Button.Disabled = false;
            entry.Button.Background = new SolidColorBrush(selected
                ? Color.FromArgb(230, 63, 164, 255)
                : Color.FromArgb(160, 39, 56, 78));
            entry.Label.TextColor = Color.FromArgb(245, 247, 250, 255);
        }
    }

    private void EnsureStageButtons(CampaignStageStateMessage[] stages)
    {
        var stageIds = stages.Select(static stage => stage.StageId).ToArray();
        if (_stageButtonSignature.SequenceEqual(stageIds))
        {
            return;
        }

        foreach (var entry in _stageButtons)
        {
            entry.Button.Dispose();
        }

        _stageButtons.Clear();
        _stageButtonSignature = stageIds;

        var chipWidth = 52f;
        var chipHeight = 24f;
        var chipGap = 6f;
        var maxPerRow = Math.Max(1, (int)((336f + chipGap) / (chipWidth + chipGap)));

        for (var index = 0; index < stages.Length; index++)
        {
            var stage = stages[index];
            var row = index / maxPerRow;
            var column = index % maxPerRow;
            var left = 12f + column * (chipWidth + chipGap);
            var top = 56f + row * (chipHeight + chipGap);
            var buttonPair = CreateButtonWithLabel(_campaignPanel!, left, top, chipWidth, chipHeight, $"S{stage.StageId}");

            var stageId = stage.StageId;
            buttonPair.Button.OnPointerClicked += (sender, e) =>
            {
                _ = sender;
                _ = e;
                _selectedStageId = Math.Max(1, stageId);
            };

            _stageButtons.Add(new StageButtonEntry(stageId, buttonPair.Button, buttonPair.Label));
        }
    }

    private void UpdateStageButtons(CampaignStageStateMessage[] stages)
    {
        foreach (var entry in _stageButtons)
        {
            var stage = stages.FirstOrDefault(candidate => candidate.StageId == entry.StageId);
            var enabled = stage is not null && stage.IsUnlocked;
            var selected = entry.StageId == _selectedStageId;

            entry.Button.Disabled = !enabled;
            entry.Button.Background = new SolidColorBrush(!enabled
                ? Color.FromArgb(120, 75, 81, 92)
                : selected
                    ? Color.FromArgb(230, 63, 164, 255)
                    : Color.FromArgb(150, 43, 62, 86));
            entry.Label.Text = $"S{entry.StageId}";
            entry.Label.TextColor = Color.FromArgb(245, 247, 250, 255);
        }
    }

    private static void SetActionButtonState(ButtonWithLabel button, bool enabled, bool highlight)
    {
        button.Button.Disabled = !enabled;
        button.Button.Background = new SolidColorBrush(enabled
            ? highlight
                ? Color.FromArgb(220, 36, 118, 196)
                : Color.FromArgb(220, 42, 108, 172)
            : Color.FromArgb(120, 72, 82, 94));
        button.Label.TextColor = Color.FromArgb(245, 247, 250, 255);
    }

    private void EnsureDefaultFontLoaded()
    {
        lock (FontGate)
        {
            if (_defaultFontId != int.MinValue)
            {
                return;
            }

            var existingFontId = Canvas.FindFont(DefaultFontName);
            if (existingFontId >= 0)
            {
                _defaultFontId = existingFontId;
                return;
            }

            foreach (var fontPath in FontPathCandidates)
            {
                var loadedFontId = Canvas.CreateFont(DefaultFontName, fontPath);
                if (loadedFontId >= 0)
                {
                    _defaultFontId = loadedFontId;
                    Game.Logger.LogInformation(
                        "AutoArmy canvas font loaded. name={FontName}, path={FontPath}, id={FontId}",
                        DefaultFontName,
                        fontPath,
                        loadedFontId);
                    return;
                }
            }

            _defaultFontId = -1;
            Game.Logger.LogWarning("AutoArmy canvas font loading failed for all candidates. text may be invisible.");
        }
    }

    private void ApplyDefaultFontFace()
    {
        if (_defaultFontId >= 0)
        {
            _canvas.FontFaceId(_defaultFontId);
        }
    }

    private void OnAnimatedRender(object? sender, GameUI.Control.Primitive.Struct.CanvasAnimatedEventArgs e)
    {
        _ = sender;
        _ = e;
        _canvas.ResetState();
        ApplyDefaultFontFace();
        var snapshot = GetLatestSnapshot();
        var campaignState = GetCampaignProgressState();
        DrawBackground(snapshot);
        DrawLane(snapshot);
        DrawUnits(snapshot);
        UpdateControlOverlay(snapshot, campaignState);
    }

    private void DrawBackground(BattleSnapshot? snapshot)
    {
        var size = ResolveCanvasSize();
        var width = size.Width;
        var height = size.Height;

        _canvas.FillPaint = new LinearGradientPaint(
            new PointF(0f, 0f),
            new PointF(0f, height),
            Color.FromArgb(255, 22, 30, 42),
            Color.FromArgb(255, 8, 13, 24));
        _canvas.FillRectangle(0f, 0f, width, height);

        _canvas.FillPaint = Color.FromArgb(60, 178, 34, 34);
        _canvas.FillRectangle(0f, 0f, width, height * 0.44f);
        _canvas.FillPaint = Color.FromArgb(60, 32, 178, 170);
        _canvas.FillRectangle(0f, height * 0.56f, width, height * 0.44f);

        if (snapshot is null)
        {
            return;
        }

        _canvas.StrokePaint = Color.FromArgb(140, 255, 255, 255);
        _canvas.StrokeWidth = 1.5f;
        _canvas.DrawLine(0f, height * 0.5f, width, height * 0.5f);
    }

    private void DrawLane(BattleSnapshot? snapshot)
    {
        var size = ResolveCanvasSize();
        var width = size.Width;
        var height = size.Height;

        var laneWidth = MathF.Min(width * 0.42f, 420f);
        var laneX = (width - laneWidth) * 0.5f;
        var laneY = StagePadding;
        var laneHeight = height - StagePadding * 2f;

        _canvas.StrokePaint = Color.FromArgb(160, 255, 255, 255);
        _canvas.StrokeWidth = 2f;
        _canvas.StrokeRoundedRectangle(laneX, laneY, laneWidth, laneHeight, 18f);

        _canvas.FillPaint = Color.FromArgb(30, 255, 255, 255);
        _canvas.FillRoundedRectangle(laneX, laneY, laneWidth, laneHeight, 18f);

        if (snapshot is null)
        {
            return;
        }

        _canvas.FillPaint = Color.FromArgb(220, 255, 255, 255);
        _canvas.FontSize = 16f;
        _canvas.TextAlign = TextAlign.Center;
        var stageText = $"Stage {snapshot.StageId} · Tick {snapshot.Tick}";
        _canvas.DrawText(width * 0.5f, StagePadding + 12f, stageText);
    }

    private void DrawUnits(BattleSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Units.Length == 0)
        {
            return;
        }

        var size = ResolveCanvasSize();
        var width = size.Width;
        var height = size.Height;
        var laneWidth = MathF.Min(width * 0.42f, 420f);
        var laneX = (width - laneWidth) * 0.5f;
        var laneTop = StagePadding + 20f;
        var laneHeight = height - StagePadding * 2f - 20f;

        foreach (var unit in snapshot.Units)
        {
            var xOffset = unit.Team == BattleTeam.Ally ? laneWidth * 0.22f : laneWidth * 0.78f;
            xOffset += RoleLateralOffset(unit.Role);
            var x = laneX + xOffset;
            var normalizedY = Math.Clamp((unit.PositionY + 4f) / 18f, 0f, 1f);
            var y = laneTop + normalizedY * laneHeight;

            var radius = unit.Kind == BattleUnitKind.Hero ? 18f : 12f;
            var bodyColor = GetRoleColor(unit.Role, unit.Team);

            _canvas.FillPaint = bodyColor;
            _canvas.FillCircle(x, y, radius);

            var castPulse = GetCastPulse(snapshot, unit.UnitId);
            if (castPulse > 0f)
            {
                _canvas.StrokePaint = Color.FromArgb((int)(220f * castPulse), 255, 246, 110);
                _canvas.StrokeWidth = 3f;
                _canvas.StrokeCircle(x, y, radius + 5f + 2f * castPulse);
            }

            if (unit.Kind == BattleUnitKind.Hero)
            {
                _canvas.StrokePaint = Color.FromArgb(255, 255, 255, 255);
                _canvas.StrokeWidth = 2f;
                _canvas.StrokeCircle(x, y, radius + 3f);
            }

            DrawHealthBar(unit, x, y, radius);
            DrawUnitTags(unit, x, y + radius + 10f);
        }
    }

    private void DrawHealthBar(BattleUnitSnapshot unit, float x, float y, float radius)
    {
        var width = 40f;
        var height = 5f;
        var left = x - width * 0.5f;
        var top = y - radius - 11f;
        var healthRatio = unit.MaxHealth <= 0f ? 0f : Math.Clamp(unit.CurrentHealth / unit.MaxHealth, 0f, 1f);

        _canvas.FillPaint = Color.FromArgb(220, 16, 20, 28);
        _canvas.FillRoundedRectangle(left, top, width, height, 2f);

        _canvas.FillPaint = Color.FromArgb(240, 91, 214, 104);
        _canvas.FillRoundedRectangle(left, top, width * healthRatio, height, 2f);
    }

    private void DrawUnitTags(BattleUnitSnapshot unit, float x, float y)
    {
        _canvas.FillPaint = Color.FromArgb(225, 230, 236, 244);
        _canvas.FontSize = 11f;
        _canvas.TextAlign = TextAlign.Center;
        _canvas.DrawText(x, y, $"{unit.Role} Lv.{unit.Level}");
    }

    private void DrawHud(BattleSnapshot? snapshot, float totalElapsedTime)
    {
        var size = ResolveCanvasSize();
        var width = size.Width;
        var height = size.Height;
        var headerY = StagePadding + 36f;
        var campaignState = GetCampaignProgressState();
        EnsureSelectedStage(campaignState);

        _canvas.FillPaint = Color.FromArgb(240, 245, 246, 250);
        _canvas.FontSize = 20f;
        _canvas.TextAlign = TextAlign.Left;
        _canvas.DrawText(StagePadding, headerY, "AUTO ARMY CAMPAIGN");

        if (snapshot is null)
        {
            _canvas.FillPaint = Color.FromArgb(220, 217, 224, 232);
            _canvas.FontSize = 15f;
            _canvas.DrawText(StagePadding, headerY + 24f, "Waiting for server snapshot...");
        }
        else
        {
            _canvas.FillPaint = Color.FromArgb(220, 217, 224, 232);
            _canvas.FontSize = 14f;
            _canvas.DrawText(StagePadding, headerY + 24f, $"Elapsed {snapshot.ElapsedSeconds:0.00}s · Ally {snapshot.AllyAliveCount} vs Enemy {snapshot.EnemyAliveCount}");

            _canvas.TextAlign = TextAlign.Right;
            var statusText = snapshot.Status == BattleStatus.Running
                ? "Status: Running"
                : $"Status: Finished · Winner: {snapshot.WinnerTeam?.ToString() ?? "None"}";
            _canvas.DrawText(width - StagePadding, headerY + 24f, statusText);

            var blink = 0.4f + 0.6f * MathF.Abs(MathF.Sin(totalElapsedTime * 5f));
            if (snapshot.VisualEvents.Any(static visualEvent => visualEvent.Type == BattleVisualEventType.UnitDied))
            {
                _canvas.FillPaint = Color.FromArgb((int)(blink * 255f), 255, 110, 110);
                _canvas.FontSize = 16f;
                _canvas.TextAlign = TextAlign.Center;
                _canvas.DrawText(width * 0.5f, StagePadding + 46f, "Unit Down!");
            }
        }

        DrawServerSelector(width);
        DrawCampaignPanel(width, height, campaignState);
        DrawOperationFeedback(width, height);
    }

    private void DrawCampaignPanel(float canvasWidth, float canvasHeight, CampaignProgressStateMessage state)
    {
        var panelWidth = MathF.Min(360f, canvasWidth * 0.35f);
        var panelHeight = MathF.Min(280f, canvasHeight * 0.43f);
        var panelX = StagePadding;
        var panelY = canvasHeight - panelHeight - StagePadding;

        _canvas.FillPaint = Color.FromArgb(210, 14, 22, 34);
        _canvas.FillRoundedRectangle(panelX, panelY, panelWidth, panelHeight, 12f);
        _canvas.StrokePaint = Color.FromArgb(150, 160, 192, 220);
        _canvas.StrokeWidth = 1.2f;
        _canvas.StrokeRoundedRectangle(panelX, panelY, panelWidth, panelHeight, 12f);

        _canvas.FillPaint = Color.FromArgb(240, 237, 242, 249);
        _canvas.FontSize = 16f;
        _canvas.TextAlign = TextAlign.Left;
        _canvas.DrawText(panelX + 12f, panelY + 24f, $"Progress · Gold {state.Gold}");

        _canvas.FillPaint = Color.FromArgb(220, 197, 214, 236);
        _canvas.FontSize = 12f;
        _canvas.DrawText(panelX + 12f, panelY + 42f, $"Flow {state.FlowState} · Unlocked Stage {state.HighestUnlockedStageId}");

        DrawStageChips(panelX + 12f, panelY + 56f, panelWidth - 24f, state);
        DrawActionButtons(panelX + 12f, panelY + 132f, panelWidth - 24f, state);

        if (state.FlowState == CampaignFlowState.ResultPendingConfirm)
        {
            var resultText = state.LastOutcome == BattleOutcome.Victory
                ? $"Result: Victory +{state.LastRewardGold} Gold"
                : $"Result: {state.LastOutcome}";
            _canvas.FillPaint = Color.FromArgb(230, 241, 224, 178);
            _canvas.FontSize = 12f;
            _canvas.TextAlign = TextAlign.Left;
            _canvas.DrawText(panelX + 12f, panelY + panelHeight - 42f, resultText);

            var confirmTop = panelY + panelHeight - 34f;
            DrawButton(
                UiActionKind.ConfirmResult,
                panelX + panelWidth - 124f,
                confirmTop,
                112f,
                24f,
                "Confirm",
                enabled: true);
        }
    }

    private void DrawStageChips(float left, float top, float availableWidth, CampaignProgressStateMessage state)
    {
        var stages = state.Stages.Length == 0
            ? [new CampaignStageStateMessage { StageId = 1, NodeId = "1-1", IsUnlocked = true }]
            : state.Stages;

        var chipWidth = 52f;
        var chipHeight = 24f;
        var chipGap = 6f;
        var maxPerRow = Math.Max(1, (int)((availableWidth + chipGap) / (chipWidth + chipGap)));

        for (var index = 0; index < stages.Length; index++)
        {
            var stage = stages[index];
            var row = index / maxPerRow;
            var column = index % maxPerRow;
            var chipLeft = left + column * (chipWidth + chipGap);
            var chipTop = top + row * (chipHeight + chipGap);
            var selected = stage.StageId == _selectedStageId;

            _canvas.FillPaint = !stage.IsUnlocked
                ? Color.FromArgb(120, 75, 81, 92)
                : selected
                    ? Color.FromArgb(230, 63, 164, 255)
                    : Color.FromArgb(150, 43, 62, 86);
            _canvas.FillRoundedRectangle(chipLeft, chipTop, chipWidth, chipHeight, 6f);

            _canvas.StrokePaint = selected
                ? Color.FromArgb(255, 220, 242, 255)
                : Color.FromArgb(110, 167, 193, 221);
            _canvas.StrokeWidth = 1.2f;
            _canvas.StrokeRoundedRectangle(chipLeft, chipTop, chipWidth, chipHeight, 6f);

            _canvas.FillPaint = Color.FromArgb(245, 247, 250, 255);
            _canvas.FontSize = 11f;
            _canvas.TextAlign = TextAlign.Center | TextAlign.Middle;
            _canvas.DrawText(chipLeft + chipWidth * 0.5f, chipTop + chipHeight * 0.55f, $"S{stage.StageId}");

            _actionHitboxes.Add(new ActionHitbox(UiActionKind.SelectStage, stage.StageId, BattleUnitRole.Guard, chipLeft, chipTop, chipWidth, chipHeight, stage.IsUnlocked));
        }
    }

    private void DrawActionButtons(float left, float top, float width, CampaignProgressStateMessage state)
    {
        var canAct = state.FlowState == CampaignFlowState.Idle;
        var selectedStageUnlocked = state.Stages.Any(stage => stage.StageId == _selectedStageId && stage.IsUnlocked);
        DrawButton(
            UiActionKind.StartStage,
            left,
            top,
            width,
            28f,
            $"Start Stage {_selectedStageId}",
            canAct && selectedStageUnlocked);

        DrawButton(
            UiActionKind.UpgradeHero,
            left,
            top + 36f,
            width,
            24f,
            $"Hero Lv.{state.HeroLevel}  Cost {state.HeroUpgradeCost}",
            canAct);
        DrawButton(
            UiActionKind.UpgradeGuard,
            left,
            top + 64f,
            width,
            24f,
            $"Guard Lv.{state.GuardLevel} Cost {state.GuardUpgradeCost}",
            canAct);
        DrawButton(
            UiActionKind.UpgradeRanger,
            left,
            top + 92f,
            width,
            24f,
            $"Ranger Lv.{state.RangerLevel} Cost {state.RangerUpgradeCost}",
            canAct);
    }

    private void DrawButton(UiActionKind action, float left, float top, float width, float height, string text, bool enabled)
    {
        _canvas.FillPaint = enabled
            ? Color.FromArgb(220, 36, 118, 196)
            : Color.FromArgb(120, 72, 82, 94);
        _canvas.FillRoundedRectangle(left, top, width, height, 7f);

        _canvas.StrokePaint = enabled
            ? Color.FromArgb(200, 191, 232, 255)
            : Color.FromArgb(90, 126, 142, 158);
        _canvas.StrokeWidth = 1.2f;
        _canvas.StrokeRoundedRectangle(left, top, width, height, 7f);

        _canvas.FillPaint = Color.FromArgb(245, 247, 250, 255);
        _canvas.FontSize = 12f;
        _canvas.TextAlign = TextAlign.Center | TextAlign.Middle;
        _canvas.DrawText(left + width * 0.5f, top + height * 0.56f, text);

        var troopRole = action switch
        {
            UiActionKind.UpgradeGuard => BattleUnitRole.Guard,
            UiActionKind.UpgradeRanger => BattleUnitRole.Ranger,
            _ => BattleUnitRole.Guard,
        };

        _actionHitboxes.Add(new ActionHitbox(action, _selectedStageId, troopRole, left, top, width, height, enabled));
    }

    private void DrawOperationFeedback(float width, float height)
    {
        var feedback = GetLatestOperationText();
        if (string.IsNullOrWhiteSpace(feedback))
        {
            return;
        }

        var boxWidth = MathF.Min(width * 0.6f, 620f);
        var boxHeight = 30f;
        var left = (width - boxWidth) * 0.5f;
        var top = height - boxHeight - 8f;

        _canvas.FillPaint = Color.FromArgb(180, 9, 16, 24);
        _canvas.FillRoundedRectangle(left, top, boxWidth, boxHeight, 9f);
        _canvas.StrokePaint = Color.FromArgb(140, 170, 191, 214);
        _canvas.StrokeWidth = 1f;
        _canvas.StrokeRoundedRectangle(left, top, boxWidth, boxHeight, 9f);

        _canvas.FillPaint = Color.FromArgb(240, 232, 241, 252);
        _canvas.FontSize = 12f;
        _canvas.TextAlign = TextAlign.Center | TextAlign.Middle;
        _canvas.DrawText(left + boxWidth * 0.5f, top + boxHeight * 0.56f, feedback);
    }

    private static float RoleLateralOffset(BattleUnitRole role)
    {
        return role switch
        {
            BattleUnitRole.Guard => -16f,
            BattleUnitRole.Striker => -5f,
            BattleUnitRole.Ranger => 8f,
            BattleUnitRole.Caster => 16f,
            _ => 0f,
        };
    }

    private static Color GetRoleColor(BattleUnitRole role, BattleTeam team)
    {
        return (team, role) switch
        {
            (BattleTeam.Ally, BattleUnitRole.Guard) => Color.FromArgb(255, 98, 170, 230),
            (BattleTeam.Ally, BattleUnitRole.Striker) => Color.FromArgb(255, 76, 208, 160),
            (BattleTeam.Ally, BattleUnitRole.Ranger) => Color.FromArgb(255, 130, 225, 110),
            (BattleTeam.Ally, BattleUnitRole.Caster) => Color.FromArgb(255, 173, 158, 255),
            (BattleTeam.Enemy, BattleUnitRole.Guard) => Color.FromArgb(255, 218, 96, 93),
            (BattleTeam.Enemy, BattleUnitRole.Striker) => Color.FromArgb(255, 238, 122, 78),
            (BattleTeam.Enemy, BattleUnitRole.Ranger) => Color.FromArgb(255, 232, 171, 64),
            (BattleTeam.Enemy, BattleUnitRole.Caster) => Color.FromArgb(255, 255, 118, 176),
            _ => Color.FromArgb(255, 210, 210, 210),
        };
    }

    private static float GetCastPulse(BattleSnapshot snapshot, int unitId)
    {
        var latestCastEvent = snapshot.VisualEvents
            .Where(visualEvent => visualEvent.Type == BattleVisualEventType.SkillCast && visualEvent.UnitId == unitId)
            .OrderByDescending(static visualEvent => visualEvent.TimestampSeconds)
            .FirstOrDefault();

        if (latestCastEvent is null)
        {
            return 0f;
        }

        var age = snapshot.ElapsedSeconds - latestCastEvent.TimestampSeconds;
        if (age < 0f || age > 0.35f)
        {
            return 0f;
        }

        return 1f - age / 0.35f;
    }

    private SizeF ResolveCanvasSize()
    {
        var size = _canvas.ActualSize;
        if (size.Width > 0f && size.Height > 0f)
        {
            return size;
        }

        return GameUI.Device.ScreenViewport.Primary.DesignResolution;
    }

    private void DrawServerSelector(float canvasWidth)
    {
        _selectorHitboxes.Clear();
        var state = GetServerSelectionState();
        var ids = state.AvailableServerIds.Length > 0 ? state.AvailableServerIds : ["S1", "S2", "S3"];
        var chipWidth = 62f;
        var chipHeight = 30f;
        var chipGap = 8f;
        var sectionWidth = ids.Length * chipWidth + (ids.Length - 1) * chipGap;
        var startX = canvasWidth - StagePadding - sectionWidth;
        var topY = StagePadding + 10f;

        _canvas.FillPaint = Color.FromArgb(210, 229, 236, 245);
        _canvas.FontSize = 12f;
        _canvas.TextAlign = TextAlign.Right;
        _canvas.DrawText(canvasWidth - StagePadding, topY - 6f, "Server");

        for (var index = 0; index < ids.Length; index++)
        {
            var serverId = ids[index];
            var left = startX + index * (chipWidth + chipGap);
            var selected = string.Equals(serverId, state.SelectedServerId, StringComparison.Ordinal);

            _canvas.FillPaint = selected
                ? Color.FromArgb(230, 63, 164, 255)
                : Color.FromArgb(160, 39, 56, 78);
            _canvas.FillRoundedRectangle(left, topY, chipWidth, chipHeight, 8f);

            _canvas.StrokePaint = selected
                ? Color.FromArgb(255, 220, 242, 255)
                : Color.FromArgb(120, 175, 196, 221);
            _canvas.StrokeWidth = 1.5f;
            _canvas.StrokeRoundedRectangle(left, topY, chipWidth, chipHeight, 8f);

            _canvas.FillPaint = Color.FromArgb(245, 247, 250, 255);
            _canvas.FontSize = 13f;
            _canvas.TextAlign = TextAlign.Center | TextAlign.Middle;
            _canvas.DrawText(left + chipWidth * 0.5f, topY + chipHeight * 0.56f, serverId);

            _selectorHitboxes.Add(new ServerSelectorHitbox(serverId, left, topY, chipWidth, chipHeight));
        }
    }

    private void OnPointerClicked(object? sender, GameUI.Control.Struct.PointerEventArgs e)
    {
        _ = sender;
        if (!e.HasPosition)
        {
            return;
        }

        var x = e.X;
        var y = e.Y;
        foreach (var hitbox in _selectorHitboxes)
        {
            if (x >= hitbox.Left &&
                x <= hitbox.Left + hitbox.Width &&
                y >= hitbox.Top &&
                y <= hitbox.Top + hitbox.Height)
            {
                _onServerSelected(hitbox.ServerId);
                return;
            }
        }

        foreach (var hitbox in _actionHitboxes)
        {
            if (!hitbox.Enabled)
            {
                continue;
            }

            if (x < hitbox.Left ||
                x > hitbox.Left + hitbox.Width ||
                y < hitbox.Top ||
                y > hitbox.Top + hitbox.Height)
            {
                continue;
            }

            switch (hitbox.Action)
            {
                case UiActionKind.SelectStage:
                    _selectedStageId = Math.Max(1, hitbox.StageId);
                    break;
                case UiActionKind.StartStage:
                    _onStartStage(Math.Max(1, _selectedStageId));
                    break;
                case UiActionKind.UpgradeHero:
                    _onUpgradeHero();
                    break;
                case UiActionKind.UpgradeGuard:
                case UiActionKind.UpgradeRanger:
                    _onUpgradeTroop(hitbox.TroopRole);
                    break;
                case UiActionKind.ConfirmResult:
                    _onConfirmResult();
                    break;
            }

            return;
        }
    }

    private void EnsureSelectedStage(CampaignProgressStateMessage state)
    {
        if (state.Stages.Length == 0)
        {
            _selectedStageId = Math.Max(1, state.CurrentStageId);
            return;
        }

        var currentSelectedUnlocked = state.Stages.Any(stage => stage.StageId == _selectedStageId && stage.IsUnlocked);
        if (currentSelectedUnlocked)
        {
            return;
        }

        var stateCurrentUnlocked = state.Stages.Any(stage => stage.StageId == state.CurrentStageId && stage.IsUnlocked);
        if (stateCurrentUnlocked)
        {
            _selectedStageId = state.CurrentStageId;
            return;
        }

        var fallback = state.Stages.FirstOrDefault(stage => stage.IsUnlocked) ?? state.Stages[0];
        _selectedStageId = Math.Max(1, fallback.StageId);
    }

    private readonly record struct ButtonWithLabel(Button Button, Label Label);

    private readonly record struct ServerButtonEntry(string ServerId, Button Button, Label Label);

    private readonly record struct StageButtonEntry(int StageId, Button Button, Label Label);

    private readonly record struct ServerSelectorHitbox(string ServerId, float Left, float Top, float Width, float Height);

    private readonly record struct ActionHitbox(
        UiActionKind Action,
        int StageId,
        BattleUnitRole TroopRole,
        float Left,
        float Top,
        float Width,
        float Height,
        bool Enabled);

    private enum UiActionKind
    {
        SelectStage = 0,
        StartStage = 1,
        UpgradeHero = 2,
        UpgradeGuard = 3,
        UpgradeRanger = 4,
        ConfirmResult = 5,
    }
}
#endif
