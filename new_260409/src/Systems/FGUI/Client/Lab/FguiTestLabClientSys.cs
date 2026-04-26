#if CLIENT
using FairyGUI;

namespace GameEntry;

public sealed partial class FguiTestLabClientSys : IGameClass
{
    private static readonly IReadOnlyList<FguiLabCatalogEntry> Entries = FguiLabCatalog.Entries;

    private static dynamic? panel;
    private static dynamic? currentLabel;
    private static dynamic? statusLabel;
    private static bool panelVisible;
    private static bool smokeRunning;
    private static int currentIndex;
    private static string currentComponentName = "N/A";
    private static GComponent? currentView;
    private static IFguiUILogic? currentUILogic;

    public static bool IsPanelVisible => panelVisible;

    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }

    private static void OnGameTriggerInitialization()
    {
        if (Game.GameModeLink != ScopeData.GameDataGameMode.MapGameMode)
        {
            return;
        }

        new Trigger<EventGameStart>(OnGameStartAsync, keepReference: true).Register(Game.Instance);
    }

    private static Task<bool> OnGameStartAsync(object sender, EventGameStart eventArgs)
    {
        EnsureUiCreated();
        return Task.FromResult(true);
    }

    public static void EnsureUiCreated()
    {
        if (panel != null)
        {
            return;
        }

        var currentLabelControl = UI.Label(string.Empty)
            .FontSize(13)
            .TextColor(Color.White);
        currentLabel = currentLabelControl;

        var statusLabelControl = UI.Label("待命")
            .FontSize(12)
            .TextColor(Color.FromArgb(210, 220, 235));
        statusLabel = statusLabelControl;

        var previousButton = UI.Button("◀")
            .Size(42, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(() => SwitchEntry(-1));

        var nextButton = UI.Button("▶")
            .Size(42, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(() => SwitchEntry(1));

        var openButton = UI.Button("显示")
            .Size(76, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(() => ShowCurrentEntry(forceReloadPackage: false));

        var reloadButton = UI.Button("重载")
            .Size(76, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(() => ShowCurrentEntry(forceReloadPackage: true));

        var closeButton = UI.Button("关闭")
            .Size(76, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(HideCurrentEntry);

        var smokeButton = UI.Button("巡检")
            .Size(76, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(RunSmoke);

        var openRunnerButton = UI.Button("旧运行器")
            .Size(92, 34)
            .Background(Color.FromArgb(220, 37, 45, 63))
            .TextColor(Color.White)
            .Click(() =>
            {
                FguiExampleRunnerClientSys.SetPanelVisible(true);
                SetPanelVisible(false);
            });

        var panelControl = UI.VStack(
                6,
                UI.Label("FGUI 测试页")
                    .FontSize(15)
                    .Bold()
                    .TextColor(Color.White),
                currentLabelControl,
                statusLabelControl,
                UI.HStack(6, previousButton, nextButton, openButton, reloadButton),
                UI.HStack(6, closeButton, smokeButton, openRunnerButton))
            .Padding(12)
            .Size(432, 176)
            .AlignTop()
            .AlignLeft()
            .Margin(12, 192, 0, 0)
            .Background(Color.FromArgb(220, 18, 24, 34))
            .CornerRadius(10);
        panel = panelControl;

        var added = panelControl.AddToVisualTree();
        Game.Logger.LogWarning("[FGUI][LAB] panel created addToTree={Added}", added);
        SetPanelVisible(false);
        RefreshPanelText();
    }

    public static bool TogglePanel()
    {
        EnsureUiCreated();
        var actualVisible = panelVisible;
        try
        {
            if (panel != null)
            {
                actualVisible = actualVisible && panel.Visible;
            }
        }
        catch
        {
            // Ignore runtime dynamic visibility probing errors and fall back to panelVisible flag.
        }

        Game.Logger.LogWarning("[FGUI][LAB] toggle before flagVisible={FlagVisible} actualVisible={ActualVisible}", panelVisible, actualVisible);
        SetPanelVisible(!actualVisible);
        Game.Logger.LogWarning("[FGUI][LAB] toggle after flagVisible={FlagVisible}", panelVisible);
        return panelVisible;
    }

    public static void SetPanelVisible(bool visible)
    {
        EnsureUiCreated();
        FGUIBootstrapClientSys.SetRootInputEnabled(visible, "fgui-test-lab");
        var currentViewMissing = currentView == null || currentView.Disposed;
        var hasPanel = panel is not null;
        Game.Logger.LogWarning(
            "[FGUI][LAB] SetPanelVisible begin visible={Visible} hasPanel={HasPanel} currentViewMissing={CurrentViewMissing}",
            visible,
            hasPanel,
            currentViewMissing);

        panelVisible = visible;
        if (panel != null)
        {
            try
            {
                if (visible)
                {
                    try
                    {
                        _ = panel.RemoveFromVisualTree();
                    }
                    catch
                    {
                        // Already removed or not in tree.
                    }

                    var added = false;
                    try
                    {
                        added = panel.AddToVisualTree();
                    }
                    catch
                    {
                        added = false;
                    }

                    panel.Visible = true;
                    Game.Logger.LogWarning("[FGUI][LAB] panel reattach addToTree={Added}", added);
                }
                else
                {
                    panel.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogWarning(ex, "[FGUI][LAB] set panel visible failed");
            }
        }

        if (!visible)
        {
            HideCurrentEntry();
            Game.Logger.LogWarning("[FGUI][LAB] SetPanelVisible end visible=false");
            return;
        }

        if (currentView == null || currentView.Disposed)
        {
            ShowCurrentEntry(forceReloadPackage: false);
        }

        if (currentView != null && !currentView.Disposed)
        {
            currentView.Visible = true;
            currentView.SortingOrder = 12000;
        }

        Game.Logger.LogWarning("[FGUI][LAB] SetPanelVisible end visible=true currentViewReady={Ready}", currentView != null && !currentView.Disposed);
    }

    private static void SwitchEntry(int delta)
    {
        if (Entries.Count == 0)
        {
            return;
        }

        currentIndex += delta;
        if (currentIndex < 0)
        {
            currentIndex = Entries.Count - 1;
        }
        else if (currentIndex >= Entries.Count)
        {
            currentIndex = 0;
        }

        ShowCurrentEntry(forceReloadPackage: true);
    }

    private static void ShowCurrentEntry(bool forceReloadPackage)
    {
        if (Entries.Count == 0)
        {
            UpdateStatus("示例清单为空");
            return;
        }

        currentIndex = Math.Clamp(currentIndex, 0, Entries.Count - 1);
        var entry = Entries[currentIndex];
        Game.Logger.LogWarning(
            "[FGUI][LAB] ShowCurrentEntry begin package={Package} forceReload={ForceReload} path={Path}",
            entry.PackageName,
            forceReloadPackage,
            entry.PackagePath);
        DestroyCurrentEntry();

        FguiUILogicRegistry.PreparePackageBindings(entry.PackageName);
        if (forceReloadPackage)
        {
            TryRemovePackage(entry.PackageName);
        }

        currentView = CreateEntryView(entry, out var resolvedComponent);
        if (currentView == null)
        {
            currentComponentName = "N/A";
            UpdateStatus($"加载失败: {entry.PackageName}");
            Game.Logger.LogWarning(
                "[FGUI][LAB] ShowCurrentEntry failed package={Package} candidateBytes={Candidates}",
                entry.PackageName,
                FGUIResourceLoader.DescribeCandidates($"{entry.PackagePath}_fui", ".bytes"));
            RefreshPanelText();
            return;
        }

        currentComponentName = resolvedComponent;
        currentView.SortingOrder = 12000;
        FitAndCenter(currentView);
        currentView.Visible = true;
        Game.Logger.LogWarning(
            "[FGUI][LAB] ShowCurrentEntry success package={Package} component={Component} size={Width}x{Height} pos={X},{Y}",
            entry.PackageName,
            resolvedComponent,
            currentView.Width,
            currentView.Height,
            currentView.X,
            currentView.Y);

        currentUILogic = FguiUILogicRegistry.Create(entry.PackageName);
        if (currentUILogic == null)
        {
            UpdateStatus($"已显示: {entry.PackageName}/{resolvedComponent} (无UI逻辑)");
            RefreshPanelText();
            return;
        }

        if (!currentUILogic.TryBind(currentView, out var bindMsg))
        {
            UpdateStatus($"UI逻辑绑定失败: {entry.PackageName} | {bindMsg}");
            RefreshPanelText();
            return;
        }

        UpdateStatus($"已显示: {entry.PackageName}/{resolvedComponent} | {bindMsg}");
        RefreshPanelText();
    }

    private static void HideCurrentEntry()
    {
        DestroyCurrentEntry();
        UpdateStatus("已关闭测试页内容");
        RefreshPanelText();
    }

    private static GComponent? CreateEntryView(FguiLabCatalogEntry entry, out string resolvedComponent)
    {
        resolvedComponent = "N/A";
        foreach (var componentName in entry.ComponentCandidates)
        {
            var view = FguiMgr.LoadToRoot(entry.PackagePath, entry.PackageName, componentName);
            if (view == null)
            {
                continue;
            }

            resolvedComponent = componentName;
            return view;
        }

        var pkg = UIRuntime.GetPackage(entry.PackageName);
        if (pkg == null)
        {
            return null;
        }

        var componentItems = pkg.GetItems()
            .Where(static x => x.Type == PackageItemType.Component)
            .OrderByDescending(static x => x.Width * x.Height)
            .ToList();
        foreach (var item in componentItems)
        {
            var fallback = pkg.CreateObject(item) as GComponent;
            if (fallback == null)
            {
                continue;
            }

            resolvedComponent = item.Name ?? item.Id ?? "N/A";
            FguiMgr.AttachToRoot(fallback, entry.PackageName, resolvedComponent);
            return fallback;
        }

        return null;
    }

    private static void RunSmoke()
    {
        if (smokeRunning)
        {
            return;
        }

        smokeRunning = true;
        var originalIndex = currentIndex;
        var failed = new List<string>();

        DestroyCurrentEntry();
        foreach (var entry in Entries)
        {
            FguiUILogicRegistry.PreparePackageBindings(entry.PackageName);
            TryRemovePackage(entry.PackageName);

            var view = CreateEntryView(entry, out _);
            if (view == null)
            {
                failed.Add($"{entry.PackageName}:load");
                continue;
            }

            var uiLogic = FguiUILogicRegistry.Create(entry.PackageName);
            if (uiLogic != null)
            {
                if (!uiLogic.TryBind(view, out var bindMsg))
                {
                    failed.Add($"{entry.PackageName}:bind({bindMsg})");
                }
                else if (!uiLogic.RunSmoke(out var smokeMsg))
                {
                    failed.Add($"{entry.PackageName}:smoke({smokeMsg})");
                }

                uiLogic.Cleanup();
            }

            try
            {
                UIRuntime.RemoveFromRoot(view, dispose: false);
                if (!view.Disposed)
                {
                    view.Dispose();
                }
            }
            catch
            {
                failed.Add($"{entry.PackageName}:dispose");
            }
        }

        smokeRunning = false;
        currentIndex = Math.Clamp(originalIndex, 0, Entries.Count - 1);
        ShowCurrentEntry(forceReloadPackage: true);

        if (failed.Count == 0)
        {
            UpdateStatus($"巡检通过: {Entries.Count}/{Entries.Count}");
            FguiNotificationBridge.EnqueueSystemTip($"FGUI 测试页巡检通过 {Entries.Count}/{Entries.Count}");
            return;
        }

        var failedText = string.Join(", ", failed);
        UpdateStatus($"巡检失败: {failed.Count} 项");
        FguiNotificationBridge.EnqueueSystemTip($"FGUI 测试页巡检失败 {failed.Count} 项");
        Game.Logger.LogWarning("[FGUI][LAB] smoke failed: {Items}", failedText);
    }

    private static void DestroyCurrentEntry()
    {
        try
        {
            currentUILogic?.Cleanup();
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "[FGUI][LAB] cleanup UI logic failed");
        }
        finally
        {
            currentUILogic = null;
        }

        if (currentView == null)
        {
            return;
        }

        try
        {
            UIRuntime.RemoveFromRoot(currentView, dispose: false);

            if (!currentView.Disposed)
            {
                currentView.Dispose();
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "[FGUI][LAB] destroy current view failed");
        }
        finally
        {
            currentView = null;
        }
    }

    private static void FitAndCenter(GComponent view)
    {
        var rootWidth = UIRuntime.RootWidth;
        var rootHeight = UIRuntime.RootHeight;
        var width = view.Width;
        var height = view.Height;
        if (width <= 1 || height <= 1)
        {
            var pkgWidth = view.PackageItem?.Width ?? 0;
            var pkgHeight = view.PackageItem?.Height ?? 0;
            if (pkgWidth > 0 && pkgHeight > 0)
            {
                width = pkgWidth;
                height = pkgHeight;
                view.SetSize(width, height, true);
            }
        }

        width = MathF.Max(1f, view.Width);
        height = MathF.Max(1f, view.Height);
        var shouldFitToVisible = UIRuntime.MatchMode != ScreenMatchMode.Fill;
        if (shouldFitToVisible)
        {
            var fitScale = MathF.Min(
                1f,
                MathF.Min(
                    (MathF.Max(1f, rootWidth) * 0.96f) / width,
                    (MathF.Max(1f, rootHeight) * 0.9f) / height));

            if (fitScale < 0.999f)
            {
                view.SetSize(width * fitScale, height * fitScale, true);
            }
        }

        view.SetXY((rootWidth - view.Width) * 0.5f, (rootHeight - view.Height) * 0.5f);
    }

    private static void TryRemovePackage(string packageName)
    {
        try
        {
            if (UIRuntime.GetPackage(packageName) != null)
            {
                UIRuntime.RemovePackage(packageName);
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "[FGUI][LAB] remove package failed: {Package}", packageName);
        }
    }

    private static void UpdateStatus(string text)
    {
        if (statusLabel != null)
        {
            statusLabel.Text = text;
        }
    }

    private static void RefreshPanelText()
    {
        if (currentLabel == null || Entries.Count == 0)
        {
            return;
        }

        var entry = Entries[currentIndex];
        currentLabel.Text = $"[{currentIndex + 1}/{Entries.Count}] {entry.PackageName} ({currentComponentName})";
    }
}
#endif




