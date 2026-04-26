#if CLIENT
using FairyGUI;

namespace GameEntry;

public sealed class FguiExampleRunnerClientSys : IGameClass
{
    private const bool EnableBasicsBitmapDigitInjection = false;
    private static readonly IReadOnlyList<FguiExampleCatalogEntry> Entries = FguiExampleCatalog.Entries;
    private static readonly IReadOnlyDictionary<char, string> HitNumberDigitUrls = new Dictionary<char, string>
    {
        // Bitmap font digits in Basics currently resolve with Sprite=null in package runtime.
        // Use scatter paths directly so digits render deterministically.
        ['0'] = "image/fgui/scatter/Basics/duef6n__h0.png",
        ['1'] = "image/fgui/scatter/Basics/duef6o__h1.png",
        ['2'] = "image/fgui/scatter/Basics/duef6p__h2.png",
        ['3'] = "image/fgui/scatter/Basics/duef6q__h3.png",
        ['4'] = "image/fgui/scatter/Basics/duef6r__h4.png",
        ['5'] = "image/fgui/scatter/Basics/duef6s__h5.png",
        ['6'] = "image/fgui/scatter/Basics/duef6t__h6.png",
        ['7'] = "image/fgui/scatter/Basics/duef6u__h7.png",
        ['8'] = "image/fgui/scatter/Basics/duef6v__h8.png",
        ['9'] = "image/fgui/scatter/Basics/duef6w__h9.png",
    };
    private static readonly IReadOnlyDictionary<char, float> HitNumberAdvanceMap = new Dictionary<char, float>
    {
        ['0'] = 33,
        ['1'] = 33,
        ['2'] = 32,
        ['3'] = 33,
        ['4'] = 33,
        ['5'] = 32,
        ['6'] = 32,
        ['7'] = 33,
        ['8'] = 33,
        ['9'] = 33,
    };

    private static string currentPanelText = string.Empty;
    private static string statusText = string.Empty;
    private static bool panelVisible;
    private static bool runnerInitialized;
    private static int currentIndex;
    private static GComponent? currentView;
    private static IFguiUILogic? currentUILogic;
    private static string currentComponentName = "N/A";
    private static bool smokeRunning;
    private static BitmapDigitTextRenderer? basicsBitmapRenderer;
    private static GComponent? basicsInjectedDemoTextView;
    private static int bitmapDigitValue;

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
        return Task.FromResult(true);
    }

    public static void EnsureUiCreated()
    {
        if (runnerInitialized)
        {
            return;
        }

        // SCE 当前运行环境与编译期 GameUI 类型契约不一致。
        // 运行器面板降级为“无控件模式”，仅保留示例加载/切换逻辑。
        runnerInitialized = true;
        panelVisible = false;
        RefreshPanelText();
    }

    public static bool TogglePanel()
    {
        SetPanelVisible(!panelVisible);
        return panelVisible;
    }

    public static bool ShowPackage(string packageName, bool forceReloadPackage = true)
    {
        EnsureUiCreated();
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return false;
        }

        var matchedIndex = -1;
        for (var i = 0; i < Entries.Count; i++)
        {
            if (!string.Equals(Entries[i].PackageName, packageName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            matchedIndex = i;
            break;
        }

        if (matchedIndex < 0)
        {
            UpdateStatus($"未找到包: {packageName}");
            RefreshPanelText();
            return false;
        }

        currentIndex = matchedIndex;
        panelVisible = true;
        FGUIBootstrapClientSys.SetRootInputEnabled(true, $"example-runner:show-package:{packageName}");
        ShowCurrentExample(forceReloadPackage);
        return currentView != null;
    }

    public static void SetPanelVisible(bool visible)
    {
        EnsureUiCreated();
        panelVisible = visible;
        FGUIBootstrapClientSys.SetRootInputEnabled(visible, "example-runner");

        if (!visible)
        {
            HideCurrentExample();
            return;
        }

        if (currentView == null)
        {
            ShowCurrentExample();
        }
    }

    private static void SwitchExample(int delta)
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

        ShowCurrentExample(forceReloadPackage: true);
    }

    private static void ShowCurrentExample(bool forceReloadPackage = false)
    {
        if (Entries.Count == 0)
        {
            UpdateStatus("示例清单为空");
            return;
        }

        var entry = Entries[currentIndex];
        DestroyCurrentView();
        FguiUILogicRegistry.PreparePackageBindings(entry.PackageName);
        var view = CreateExampleView(entry, forceReloadPackage, out var resolvedComponent);
        if (view == null)
        {
            currentComponentName = "N/A";
            UpdateStatus($"加载失败: {entry.PackageName}");
            RefreshPanelText();
            FguiNotificationBridge.EnqueueSystemTip($"FGUI 示例加载失败: {entry.PackageName}");
            return;
        }

        currentView = view;
        currentComponentName = resolvedComponent;
        currentView.SortingOrder = 9998;
        FitAndCenter(currentView);
        currentView.Visible = true;
        TryAttachBitmapDigitDemo(entry, currentView);
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

    private static void HideCurrentExample()
    {
        DestroyCurrentView();
        UpdateStatus("已关闭示例视图");
        RefreshPanelText();
    }

    private static void RunSmokeTest()
    {
        if (smokeRunning)
        {
            return;
        }

        smokeRunning = true;
        var originalIndex = currentIndex;
        var failed = new List<string>();

        DestroyCurrentView();
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            FguiUILogicRegistry.PreparePackageBindings(entry.PackageName);
            var view = CreateExampleView(entry, forceReloadPackage: true, out _);
            if (view == null)
            {
                failed.Add(entry.PackageName);
                continue;
            }

            try
            {
                var uiLogic = FguiUILogicRegistry.Create(entry.PackageName);
                try
                {
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
                    }
                }
                finally
                {
                    uiLogic?.Cleanup();
                }

                view.RemoveFromParent();
                view.Dispose();
            }
            catch
            {
                // Ignore cleanup failures in smoke mode.
            }
        }

        smokeRunning = false;
        currentIndex = Math.Clamp(originalIndex, 0, Entries.Count - 1);
        ShowCurrentExample(forceReloadPackage: true);

        if (failed.Count == 0)
        {
            UpdateStatus($"巡检通过: {Entries.Count}/{Entries.Count}");
            FguiNotificationBridge.EnqueueSystemTip($"FGUI 示例巡检通过 {Entries.Count}/{Entries.Count}");
        }
        else
        {
            var failedPackages = string.Join(",", failed);
            UpdateStatus($"巡检失败: {failed.Count} 个 ({failedPackages})");
            FguiNotificationBridge.EnqueueSystemTip($"FGUI 示例巡检失败 {failed.Count} 个");
            Game.Logger.LogWarning("[FGUI][RUNNER] Smoke failed packages: {Packages}", failedPackages);
        }
    }

    private static GComponent? CreateExampleView(
        FguiExampleCatalogEntry entry,
        bool forceReloadPackage,
        out string resolvedComponent)
    {
        resolvedComponent = "N/A";
        if (forceReloadPackage)
        {
            TryRemovePackage(entry.PackageName);
        }

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

        var fallback = CreateBestEffortComponent(pkg, out resolvedComponent);
        if (fallback == null)
        {
            return null;
        }

        FguiMgr.AttachToRoot(fallback, entry.PackageName, resolvedComponent);
        return fallback;
    }

    private static GComponent? CreateBestEffortComponent(UIPackage pkg, out string resolvedComponent)
    {
        resolvedComponent = "N/A";
        var componentItems = pkg.GetItems()
            .Where(static item => item.Type == PackageItemType.Component)
            .OrderByDescending(static item => ScoreComponent(item))
            .ToList();

        foreach (var item in componentItems)
        {
            var objectInstance = pkg.CreateObject(item) as GComponent;
            if (objectInstance == null)
            {
                continue;
            }

            resolvedComponent = string.IsNullOrWhiteSpace(item.Name) ? (item.Id ?? "N/A") : item.Name;
            return objectInstance;
        }

        return null;
    }

    private static float ScoreComponent(PackageItem item)
    {
        var score = (float)item.Width * item.Height;
        var name = item.Name ?? string.Empty;
        if (name.Equals("Main", StringComparison.OrdinalIgnoreCase))
        {
            score += 1_000_000f;
        }
        else if (name.Contains("main", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("demo", StringComparison.OrdinalIgnoreCase))
        {
            score += 300_000f;
        }

        if (name.Contains("item", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("button", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("close", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("scrollbar", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("windowframe", StringComparison.OrdinalIgnoreCase))
        {
            score -= 200_000f;
        }

        if (item.Width < 120 || item.Height < 120)
        {
            score -= 100_000f;
        }

        return score;
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
                view.SetSize(width, height);
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
                view.SetSize(width * fitScale, height * fitScale);
            }
        }

        view.SetXY((rootWidth - view.Width) * 0.5f, (rootHeight - view.Height) * 0.5f);
    }

    private static void DestroyCurrentView()
    {
        try
        {
            currentUILogic?.Cleanup();
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "[FGUI][RUNNER] cleanup UI logic failed");
        }
        finally
        {
            currentUILogic = null;
        }

        DisposeBitmapDigitDemo();
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
            Game.Logger.LogWarning(ex, "[FGUI][RUNNER] DestroyCurrentView failed");
        }
        finally
        {
            currentView = null;
        }
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
            Game.Logger.LogWarning(ex, "[FGUI][RUNNER] remove package failed: {Package}", packageName);
        }
    }

    private static void TryAttachBitmapDigitDemo(FguiExampleCatalogEntry entry, GComponent view)
    {
        DisposeBitmapDigitDemo();
        if (!EnableBasicsBitmapDigitInjection)
        {
            return;
        }

        if (!string.Equals(entry.PackageName, "Basics", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryFindBasicsHitNumberTarget(view, out var host, out var textField))
        {
            if (!TryCreateAndAttachBasicsDemoTextView(view, out host, out textField))
            {
                Game.Logger.LogInformation("[FGUI][BitmapDigit] Basics target n7 not found.");
                return;
            }
        }
        else if (!host.FinalVisible || !textField.FinalVisible)
        {
            if (!TryCreateAndAttachBasicsDemoTextView(view, out host, out textField))
            {
                Game.Logger.LogInformation(
                    "[FGUI][BitmapDigit] found n7 but hidden and fallback create failed. hostFinal={HostFinal} textFinal={TextFinal}",
                    host.FinalVisible, textField.FinalVisible);
                return;
            }
        }

        PlaceBitmapDigitTargetLeftOfBackButton(view, host, textField);
        ResetBitmapDigitSeedText(textField);
        bitmapDigitValue = 0;
        basicsBitmapRenderer = new BitmapDigitTextRenderer(view, textField, HitNumberDigitUrls, HitNumberAdvanceMap);
        basicsBitmapRenderer.SetText(bitmapDigitValue.ToString());
        Game.Logger.LogInformation("[FGUI][BitmapDigit] Attached to Basics Demo_Text.n7.");
    }

    private static void PlaceBitmapDigitTargetLeftOfBackButton(GComponent root, GComponent host, GTextField textField)
    {
        var backButton = FindObjectByName(root, "btn_Back");
        if (backButton == null)
        {
            PlaceBitmapDigitTargetAtMiddleTop(host, textField);
            return;
        }

        GetAbsolutePosition(backButton, out var backAbsX, out var backAbsY);
        GetAbsolutePosition(host, out var hostAbsX, out var hostAbsY);

        var targetWidth = textField.Width > 1 ? textField.Width : 345;
        var targetHeight = textField.Height > 1 ? textField.Height : 54;
        var hostWidth = host.Width > 1 ? host.Width : root.Width;
        var hostHeight = host.Height > 1 ? host.Height : root.Height;

        const float horizontalGap = 12f;
        var localX = backAbsX - hostAbsX - targetWidth - horizontalGap;
        var localY = backAbsY - hostAbsY + (backButton.Height - targetHeight) * 0.5f;
        if (hostWidth > 1f)
        {
            var maxX = MathF.Max(0f, hostWidth - targetWidth);
            localX = Math.Clamp(localX, 0f, maxX);
        }
        else
        {
            localX = MathF.Max(0f, localX);
        }

        if (hostHeight > 1f)
        {
            var maxY = MathF.Max(0f, hostHeight - targetHeight);
            localY = Math.Clamp(localY, 0f, maxY);
        }
        else
        {
            localY = MathF.Max(0f, localY);
        }

        textField.SetXY(localX, localY);
    }

    private static void ResetBitmapDigitSeedText(GTextField textField)
    {
        textField.Text = "0";
    }

    private static void PlaceBitmapDigitTargetAtMiddleTop(GComponent host, GTextField textField)
    {
        var hostWidth = host.Width > 1 ? host.Width : (currentView?.Width ?? 0);
        if (hostWidth <= 1)
        {
            hostWidth = 1136;
        }

        var targetWidth = textField.Width > 1 ? textField.Width : 345;
        var x = MathF.Max(0f, (hostWidth - targetWidth) * 0.5f);
        const float topMargin = 24f;

        textField.SetXY(x, topMargin);
    }

    private static GObject? FindObjectByName(GComponent root, string name)
    {
        var stack = new Stack<GObject>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            if (current is not GComponent comp)
            {
                continue;
            }

            for (var i = comp.NumChildren - 1; i >= 0; i--)
            {
                stack.Push(comp.GetChildAt(i));
            }
        }

        return null;
    }

    private static void GetAbsolutePosition(GObject obj, out float x, out float y)
    {
        x = obj.X;
        y = obj.Y;

        var parent = obj.Parent;
        while (parent != null)
        {
            x += parent.X;
            y += parent.Y;
            parent = parent.Parent;
        }
    }

    private static void DisposeBitmapDigitDemo()
    {
        if (basicsBitmapRenderer != null)
        {
            basicsBitmapRenderer.Dispose();
            basicsBitmapRenderer = null;
        }

        if (basicsInjectedDemoTextView != null)
        {
            try
            {
                if (basicsInjectedDemoTextView.Parent != null)
                {
                    basicsInjectedDemoTextView.RemoveFromParent();
                }

                if (!basicsInjectedDemoTextView.Disposed)
                {
                    basicsInjectedDemoTextView.Dispose();
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogWarning(ex, "[FGUI][BitmapDigit] dispose injected Demo_Text failed.");
            }
            finally
            {
                basicsInjectedDemoTextView = null;
            }
        }
    }

    private static bool TryCreateAndAttachBasicsDemoTextView(
        GComponent root,
        out GComponent host,
        out GTextField textField)
    {
        host = root;
        textField = null!;

        GComponent? demoTextView = null;
        try
        {
            demoTextView = UIRuntime.CreateObject("Basics", "Demo_Text") as GComponent;
            if (demoTextView == null)
            {
                demoTextView = UIPackage.CreateObject("Basics", "Demo_Text") as GComponent;
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning(ex, "[FGUI][BitmapDigit] create Basics/Demo_Text failed.");
        }

        if (demoTextView == null)
        {
            return false;
        }

        root.AddChild(demoTextView);
        demoTextView.SetXY(0, 0);
        var viewWidth = root.Width > 1 ? root.Width : 1136;
        var viewHeight = root.Height > 1 ? root.Height : 570;
        demoTextView.SetSize(viewWidth, viewHeight, true);

        demoTextView.SortingOrder = 9999;
        basicsInjectedDemoTextView = demoTextView;

        if (!TryFindBasicsHitNumberTarget(demoTextView, out host, out textField))
        {
            if (demoTextView.Parent != null)
            {
                demoTextView.RemoveFromParent();
            }

            demoTextView.Dispose();
            basicsInjectedDemoTextView = null;
            return false;
        }

        return true;
    }

    private static bool TryFindBasicsHitNumberTarget(
        GComponent root,
        out GComponent host,
        out GTextField textField)
    {
        host = root;
        textField = null!;

        var stack = new Stack<GObject>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is GTextField tf &&
                string.Equals(tf.Name, "n7", StringComparison.OrdinalIgnoreCase) &&
                (tf.Font.Contains("duef6m", StringComparison.OrdinalIgnoreCase) ||
                 (tf.Text ?? string.Empty).Contains("1234567890", StringComparison.OrdinalIgnoreCase)))
            {
                var parent = tf.Parent as GComponent;
                if (parent == null)
                {
                    continue;
                }

                host = parent;
                textField = tf;
                return true;
            }

            if (current is not GComponent comp)
            {
                continue;
            }

            for (var i = comp.NumChildren - 1; i >= 0; i--)
            {
                stack.Push(comp.GetChildAt(i));
            }
        }

        return false;
    }

    private static void UpdateStatus(string text)
    {
        statusText = text;
        Game.Logger.LogInformation("[FGUI][RUNNER] {Status}", text);
    }

    private static void RefreshPanelText()
    {
        if (Entries.Count == 0)
        {
            return;
        }

        var entry = Entries[currentIndex];
        currentPanelText = $"[{currentIndex + 1}/{Entries.Count}] {entry.PackageName} ({currentComponentName})";
    }
}
#endif


