#if CLIENT
using System.Diagnostics;
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class BasicsUILogic : FguiUILogicBase
{
    private Basics.Main? main;
    private PopupMenu? popupMenu;
    private GComponent? popupComponent;
    private GComponent? windowAComponent;
    private GComponent? windowBComponent;
    private GComponent? runtimeDemoHost;
    private readonly Dictionary<string, GComponent> demoCache = new(StringComparer.Ordinal);
    private readonly HashSet<GComponent> demoBehaviorBound = [];
    private readonly Dictionary<string, EventCallback> popupTriggerClickCallbacks = new(StringComparer.Ordinal);
    private readonly HashSet<string> windowCloseBindingKeys = new(StringComparer.Ordinal);
    private long popupMenuLastToggleTickMs = long.MinValue;
    private const long PopupMenuDuplicateClickSuppressMs = 80;
    private bool popupMenuToggleInProgress;
    private long popupAnyLastToggleTickMs = long.MinValue;
    private const long PopupAnyDuplicateClickSuppressMs = 80;
    private bool popupAnyToggleInProgress;
    private PointF depthStartPos;
    private const bool EnableBasicsPerfLogs = false;
    private const bool EnableDemoPrewarm = true;
    private const bool EnableDemoLifecycleLogs = false;
    private const int BasicsPerfLogLimit = 120;
    private static readonly string[] DemoPrewarmKeys = ["Grid", "List"];
    private int basicsPerfLogCount;
    private int demoPrewarmRevision;
    private int demoPrewarmCursor;

    public override string PackageName => "Basics";

    public override bool TryBind(GComponent view, out string message)
    {
        if (view is not Basics.Main typed)
        {
            message = "bind failed: view is not Basics.Main (check binder/export).";
            return false;
        }

        main = typed;
        main.c1.SelectedIndex = 0;
        main.btn_Back.Visible = false;
        main.btn_Back.OnClick.Add(OnBackClick);
        BindMainButtons();
        LogMainButtonGroupBinding();
        LogMainN26AndScreenMetrics();
        StartDemoPrewarm();

        message = "Basics bound (strong type).";
        return true;
    }

    public override bool RunSmoke(out string message)
    {
        if (main == null)
        {
            message = "smoke failed: not bound";
            return false;
        }

        return TryShowDemo("Button", out message);
    }

    public override void Cleanup()
    {
        popupMenu?.Dispose();
        popupMenu = null;
        if (popupComponent != null)
        {
            if (popupComponent.Parent != null)
            {
                popupComponent.RemoveFromParent();
            }

            if (!popupComponent.Disposed)
            {
                popupComponent.Dispose();
            }
        }

        popupComponent = null;
        DisposeWindowComponent(ref windowAComponent);
        DisposeWindowComponent(ref windowBComponent);
        windowCloseBindingKeys.Clear();
        demoPrewarmRevision++;
        demoPrewarmCursor = 0;
        DisposeCachedDemos();
        DestroyRuntimeDemoHost();
        popupTriggerClickCallbacks.Clear();
        main = null;
    }

    private void BindMainButtons()
    {
        if (main == null)
        {
            return;
        }

        for (var i = 0; i < main.NumChildren; i++)
        {
            if (main.GetChildAt(i) is not GButton button)
            {
                continue;
            }

            if (button == main.btn_Back)
            {
                continue;
            }

            if (!button.Name.StartsWith("btn_", StringComparison.Ordinal))
            {
                continue;
            }

            // Basics demo page flow is controlled by runtime logic.
            // Clear exporter-linked controller routing to avoid button click overriding c1 unexpectedly.
            button.RelatedController = null;
            button.RelatedPageId = null;

            var captured = button;
            Game.Logger.LogInformation(
                "[FGUI][Basics][BTN-BIND] name={Name} visible={Visible} final={FinalVisible} touchable={Touchable} parent={Parent}",
                captured.Name,
                captured.Visible,
                captured.FinalVisible,
                captured.Touchable,
                captured.Parent?.Name ?? "<none>");
            captured.OnClick.Add(_ => OnMainButtonClick(captured));
        }
    }

    private void OnMainButtonClick(GButton button)
    {
        if (main == null)
        {
            return;
        }

        Game.Logger.LogInformation(
            "[FGUI][Basics][BTN-CLICK] name={Name} visible={Visible} final={FinalVisible} touchable={Touchable} c1={C1}",
            button.Name,
            button.Visible,
            button.FinalVisible,
            button.Touchable,
            main.c1.SelectedIndex);

        ForceMainPage(1, $"btn:{button.Name}:pre");
        main.btn_Back.Visible = true;
        var key = button.Name.StartsWith("btn_", StringComparison.Ordinal)
            ? button.Name.Substring(4)
            : button.Name;
        if (key.Equals("Component", StringComparison.Ordinal))
        {
            key = "Transition";
        }

        if (!TryShowDemo(key, out var message))
        {
            Game.Logger.LogWarning("[FGUI][Basics] failed to show demo key={Key}. reason={Reason}", key, message);
        }

        ForceMainPage(1, $"btn:{button.Name}:post");
    }

    private void OnBackClick(EventContext _)
    {
        if (main == null)
        {
            return;
        }

        popupMenu?.Hide();
        CloseAllComboDropdownsInBasics();
        HidePopupComponentOverlay();
        ForceMainPage(0, "back");
        main.btn_Back.Visible = false;
        runtimeDemoHost?.RemoveChildren(0, -1, dispose: false);
    }

    private void CloseAllComboDropdownsInBasics()
    {
        if (runtimeDemoHost != null)
        {
            CloseComboDropdownsRecursive(runtimeDemoHost);
        }

        foreach (var demo in demoCache.Values)
        {
            if (!demo.Disposed)
            {
                CloseComboDropdownsRecursive(demo);
            }
        }
    }

    private static void CloseComboDropdownsRecursive(GObject node)
    {
        if (node is GComboBox combo)
        {
            combo.CloseDropdownByOwnerDetach();
        }

        if (node is not GComponent component)
        {
            return;
        }

        for (var i = 0; i < component.NumChildren; i++)
        {
            CloseComboDropdownsRecursive(component.GetChildAt(i));
        }
    }

    private bool TryShowDemo(string key, out string message)
    {
        message = string.Empty;
        if (main == null)
        {
            message = $"demo {key}: main missing";
            return false;
        }

        var demo = GetOrCreateDemoComponent(key, out var createdNow);
        if (demo == null)
        {
            message = $"demo {key}: component not found";
            return false;
        }

        if (!EnsureRuntimeDemoHost() || runtimeDemoHost == null)
        {
            message = $"demo {key}: runtime host missing";
            return false;
        }

        popupMenu?.Hide();
        main.SetChildIndex(runtimeDemoHost, Math.Max(0, main.NumChildren - 1));
        runtimeDemoHost.RemoveChildren(0, -1, dispose: false);
        runtimeDemoHost.AddChild(demo);
        ForceMainPage(1, $"show:{key}");
        main.btn_Back.Visible = true;
        var behaviorBoundNow = EnsureDemoBehaviorBound(demo);
        if (behaviorBoundNow)
        {
            ApplyDemoBehavior(key, demo);
        }

        if (EnableDemoLifecycleLogs)
        {
            Game.Logger.LogWarning(
                "[FGUI][DEMO] show key={Key} created={Created} behaviorBoundNow={BehaviorBoundNow} hostChildren={HostChildren}",
                key,
                createdNow,
                behaviorBoundNow,
                runtimeDemoHost.NumChildren);
        }

        message = $"demo {key}: shown in runtime host";
        return true;
    }

    private GComponent? GetOrCreateDemoComponent(string key, out bool createdNow)
    {
        createdNow = false;
        if (demoCache.TryGetValue(key, out var cached) && !cached.Disposed)
        {
            return cached;
        }

        var created = CreateDemoComponentByKey(key);
        if (created == null)
        {
            return null;
        }

        demoCache[key] = created;
        createdNow = true;
        return created;
    }

    private void StartDemoPrewarm()
    {
        if (!EnableDemoPrewarm || main == null || DemoPrewarmKeys.Length == 0)
        {
            return;
        }

        demoPrewarmRevision++;
        demoPrewarmCursor = 0;
        var revision = demoPrewarmRevision;
        GTween.DelayedCall(0.02f, () => RunDemoPrewarmStep(revision));
    }

    private void RunDemoPrewarmStep(int revision)
    {
        if (!EnableDemoPrewarm || main == null || revision != demoPrewarmRevision)
        {
            return;
        }

        if (demoPrewarmCursor >= DemoPrewarmKeys.Length)
        {
            return;
        }

        var key = DemoPrewarmKeys[demoPrewarmCursor++];
        var start = Stopwatch.GetTimestamp();
        var demo = GetOrCreateDemoComponent(key, out var createdNow);
        var behaviorBoundNow = false;
        if (demo != null)
        {
            behaviorBoundNow = EnsureDemoBehaviorBound(demo);
            if (behaviorBoundNow)
            {
                ApplyDemoBehavior(key, demo);
            }
        }

        var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        if (EnableDemoLifecycleLogs)
        {
            Game.Logger.LogWarning(
                "[FGUI][PREWARM] key={Key} created={Created} behaviorBoundNow={BehaviorBoundNow} elapsedMs={ElapsedMs:F2} step={Step}/{Total}",
                key,
                createdNow,
                behaviorBoundNow,
                elapsedMs,
                demoPrewarmCursor,
                DemoPrewarmKeys.Length);
        }

        if (demoPrewarmCursor < DemoPrewarmKeys.Length)
        {
            GTween.DelayedCall(0.02f, () => RunDemoPrewarmStep(revision));
        }
    }

    private bool EnsureDemoBehaviorBound(GComponent demo)
    {
        if (demoBehaviorBound.Contains(demo))
        {
            return false;
        }

        demoBehaviorBound.Add(demo);
        return true;
    }

    private void ForceMainPage(int index, string reason)
    {
        if (main?.c1 == null)
        {
            return;
        }

        var controller = main.c1;
        var oldIndex = controller.SelectedIndex;
        var oldPage = controller.SelectedPage;
        var oldPageId = controller.SelectedPageId;
        if (oldIndex != index)
        {
            controller.SelectedIndex = index;
        }

        Game.Logger.LogInformation(
            "[FGUI][Basics][C1] reason={Reason} index={OldIndex}->{NewIndex} page={OldPage}->{NewPage} pageId={OldPageId}->{NewPageId}",
            reason,
            oldIndex,
            controller.SelectedIndex,
            oldPage,
            controller.SelectedPage,
            oldPageId,
            controller.SelectedPageId);

        Game.Logger.LogInformation(
            "[FGUI][Basics][C1][VIS] reason={Reason} btnsVisible={BtnsVisible} btnsFinal={BtnsFinal} btnButtonFinal={BtnButtonFinal} btnControllerFinal={BtnControllerFinal}",
            reason,
            main.btns.Visible,
            main.btns.FinalVisible,
            main.btn_Button.FinalVisible,
            main.btn_Controller.FinalVisible);
    }

    private void LogMainButtonGroupBinding()
    {
        if (main == null)
        {
            return;
        }

        var total = 0;
        var grouped = 0;
        var ungrouped = new List<string>();
        for (var i = 0; i < main.NumChildren; i++)
        {
            if (main.GetChildAt(i) is not GButton button)
            {
                continue;
            }

            if (!button.Name.StartsWith("btn_", StringComparison.Ordinal))
            {
                continue;
            }

            total++;
            if (button.Group == main.btns)
            {
                grouped++;
            }
            else
            {
                ungrouped.Add(button.Name);
            }
        }

        Game.Logger.LogInformation(
            "[FGUI][Basics][GroupBind] totalButtons={Total} groupedToBtns={Grouped} groupName={GroupName} ungrouped=[{Ungrouped}]",
            total,
            grouped,
            main.btns.Name,
            string.Join(",", ungrouped));
    }

    private void LogMainN26AndScreenMetrics()
    {
        if (main == null)
        {
            return;
        }

        var n26 = main.GetChild("n26");
        var hasAdapter = UIRuntime.Adapter != null;
        var screenSize = hasAdapter ? UIRuntime.Adapter!.GetScreenSize() : SizeF.Empty;

        Game.Logger.LogInformation(
            "[FGUI][Basics][N26] found={Found} n26Size={N26Width}x{N26Height} n26Pos={N26X},{N26Y} screen={ScreenWidth}x{ScreenHeight} root={RootWidth}x{RootHeight}",
            n26 != null,
            n26?.Width ?? -1f,
            n26?.Height ?? -1f,
            n26?.X ?? -1f,
            n26?.Y ?? -1f,
            hasAdapter ? screenSize.Width : -1f,
            hasAdapter ? screenSize.Height : -1f,
            UIRuntime.RootWidth,
            UIRuntime.RootHeight);
    }

    private bool EnsureRuntimeDemoHost()
    {
        if (main == null)
        {
            return false;
        }

        if (runtimeDemoHost is { Disposed: false, Parent: not null })
        {
            return true;
        }

        DestroyRuntimeDemoHost();
        var host = new GComponent
        {
            Name = "runtime_demo_host",
        };
        host.SetXY(0f, 70f);
        host.SetSize(Math.Max(1f, main.Width), Math.Max(1f, main.Height - 70f), true);
        host.InitRelations();
        host.Relations?.Add(main, RelationType.Size);
        main.AddChild(host);
        runtimeDemoHost = host;
        return true;
    }

    private void DestroyRuntimeDemoHost()
    {
        if (runtimeDemoHost == null)
        {
            return;
        }

        try
        {
            runtimeDemoHost.RemoveChildren(0, -1, dispose: false);
            if (runtimeDemoHost.Parent != null)
            {
                runtimeDemoHost.RemoveFromParent();
            }

            if (!runtimeDemoHost.Disposed)
            {
                runtimeDemoHost.Dispose();
            }
        }
        catch
        {
        }
        finally
        {
            runtimeDemoHost = null;
        }
    }

    private void DisposeCachedDemos()
    {
        if (demoCache.Count == 0)
        {
            return;
        }

        foreach (var demo in demoCache.Values)
        {
            if (demo.Disposed)
            {
                continue;
            }

            if (demo.Parent != null)
            {
                demo.RemoveFromParent();
            }

            demo.Dispose();
        }

        demoCache.Clear();
        demoBehaviorBound.Clear();
    }

    private static GComponent? CreateDemoComponentByKey(string key)
    {
        var normalized = NormalizeToken(key);
        var candidates = key switch
        {
            "DragDrop" => new[] { "Demo_Drag_Drop", "Demo_DragDrop", "Demo_Drag&Drop" },
            "ProgressBar" => new[] { "Demo_ProgressBar" },
            _ => new[] { $"Demo_{key}" },
        };

        foreach (var candidate in candidates)
        {
            var obj = UIPackage.CreateObject("Basics", candidate) as GComponent;
            if (obj != null)
            {
                return obj;
            }
        }

        var pkg = UIRuntime.GetPackage("Basics");
        if (pkg == null)
        {
            return null;
        }

        foreach (var item in pkg.GetItems())
        {
            if (item.Type != PackageItemType.Component || string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            if (!item.Name.StartsWith("Demo_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!NormalizeToken(item.Name).Contains(normalized, StringComparison.Ordinal))
            {
                continue;
            }

            if (pkg.CreateObject(item) is GComponent matched)
            {
                return matched;
            }
        }

        return null;
    }

    private void ApplyDemoBehavior(string key, GComponent demo)
    {
        var perfStart = EnableBasicsPerfLogs ? Stopwatch.GetTimestamp() : 0L;
        switch (key)
        {
            case "Button":
                PlayButton(demo);
                break;
            case "Text":
                PlayText(demo);
                break;
            case "Grid":
                PlayGrid(demo);
                break;
            case "List":
                PlayList(demo);
                break;
            case "Controller":
                PlayController(demo);
                break;
            case "Graph":
                PlayGraph(demo);
                break;
            case "Popup":
                PlayPopup(demo);
                break;
            case "Transition":
                PlayTransition(demo);
                break;
            case "Window":
                PlayWindow(demo);
                break;
            case "DragDrop":
                PlayDragDrop(demo);
                break;
            case "Depth":
                PlayDepth(demo);
                break;
            case "ProgressBar":
                PlayProgressBar(demo);
                break;
        }

        if (EnableBasicsPerfLogs)
        {
            var elapsedMs = (Stopwatch.GetTimestamp() - perfStart) * 1000.0 / Stopwatch.Frequency;
            LogBasicsPerf(key, elapsedMs, demo);
        }
    }

    private void LogBasicsPerf(string key, double elapsedMs, GComponent demo)
    {
        if (basicsPerfLogCount >= BasicsPerfLogLimit)
        {
            return;
        }

        var shouldLog =
            key == "Grid" || key == "List" || key == "Button" ||
            basicsPerfLogCount < 10 || elapsedMs >= 8.0;
        if (!shouldLog)
        {
            return;
        }

        basicsPerfLogCount++;
        Game.Logger.LogWarning(
            "[FGUI][BASICS-PERF] key={Key} elapsedMs={ElapsedMs:F2} demo={Demo} children={Children} sample={Sample}",
            key,
            elapsedMs,
            demo.PackageItem?.Name ?? demo.Name,
            demo.NumChildren,
            basicsPerfLogCount);
    }

    private static void PlayButton(GComponent demo)
    {
        if (FindChildRecursive(demo, "n34") is GObject n34)
        {
            n34.OnClick.Add(_ => Game.Logger.LogInformation("[FGUI][Basics] Button demo n34 clicked"));
        }
    }

    private static void PlayText(GComponent demo)
    {
        if (FindChildRecursive(demo, "n12") is GRichTextField rich)
        {
            rich.OnClickLink.Add(ctx =>
            {
                var link = ctx.Data?.ToString() ?? string.Empty;
                rich.Text = $"[img]ui://Basics/pet[/img][color=#FF0000]You click the link[/color]: {link}";
            });
        }

        if (FindChildRecursive(demo, "n25") is GObject n25 &&
            FindChildRecursive(demo, "n22") is GTextField n22 &&
            FindChildRecursive(demo, "n24") is GTextField n24)
        {
            Game.Logger.LogInformation("[FGUI][Basics][Text] bound n25 click: n22 -> n24");
            n25.OnClick.Add(_ =>
            {
                var input = n22.Text ?? string.Empty;
                var before = n24.Text ?? string.Empty;
                n24.Text = input;
                var after = n24.Text ?? string.Empty;
                Game.Logger.LogInformation($"[FGUI][Basics][Text] n25 clicked input='{input}' before='{before}' after='{after}'");
            });
        }
    }

    private static void PlayList(GComponent demo)
    {
        _ = demo;
    }

    private static void PlayGrid(GComponent demo)
    {
        var list1 = FindListByName(demo, "list1");
        var list2 = FindListByName(demo, "list2");
        if (list1 == null || list2 == null)
        {
            return;
        }

        var names = new[]
        {
            "Windows", "Linux", "macOS", "Android", "iOS",
            "Switch", "PlayStation", "Xbox", "Web", "Wasm"
        };
        var colors = new[]
        {
            Color.Yellow, Color.Red, Color.White, Color.Cyan
        };
        var targetCount = ResolveGridTargetCount(list1, list2, names.Length);

        list1.BeginUpdate();
        list2.BeginUpdate();
        try
        {
            EnsureListButtonCount(list1, targetCount);
            EnsureListButtonCount(list2, targetCount);
            for (var i = 0; i < targetCount; i++)
            {
                var itemName = names[i % names.Length];
                var item1 = list1.GetChildAt(i) as GButton;
                if (item1 != null)
                {
                    SetChildTextIfChanged(item1, "t0", $"{i + 1}");
                    SetChildTextIfChanged(item1, "t1", itemName);
                    if (item1.GetChild("t2") is GTextField t2)
                    {
                        var color = colors[i % colors.Length];
                        if (t2.Color != color)
                        {
                            t2.Color = color;
                        }
                    }

                    if (item1.GetChild("star") is GProgressBar star)
                    {
                        var starValue = (i % 4 + 1) * 25;
                        if (star.Value != starValue)
                        {
                            star.Value = starValue;
                        }
                    }
                }

                var item2 = list2.GetChildAt(i) as GButton;
                if (item2 == null)
                {
                    continue;
                }

                if (item2.GetChild("cb") is GButton cb)
                {
                    if (cb.Selected)
                    {
                        cb.Selected = false;
                    }
                }

                SetChildTextIfChanged(item2, "t1", itemName);
                if (item2.GetChild("mc") is GMovieClip mc)
                {
                    var shouldPlay = i % 2 == 0;
                    if (mc.Playing != shouldPlay)
                    {
                        mc.Playing = shouldPlay;
                    }
                }

                SetChildTextIfChanged(item2, "t3", $"{1000 + i * 13}");
            }
        }
        finally
        {
            list1.EndUpdate();
            list2.EndUpdate();
        }
    }

    private static int ResolveGridTargetCount(GList list1, GList list2, int fallbackCount)
    {
        var existing = Math.Max(list1.NumChildren, list2.NumChildren);
        if (existing > 0)
        {
            return existing;
        }

        return Math.Max(1, fallbackCount);
    }

    private static void EnsureListButtonCount(GList list, int targetCount)
    {
        var current = list.NumChildren;
        if (current > targetCount)
        {
            list.RemoveChildrenToPool(targetCount, current);
            current = targetCount;
        }

        for (var i = current; i < targetCount; i++)
        {
            list.AddItemFromPool();
        }
    }

    private static void SetChildTextIfChanged(GComponent owner, string childName, string value)
    {
        var child = owner.GetChild(childName);
        if (child == null)
        {
            return;
        }

        if (!string.Equals(child.Text, value, StringComparison.Ordinal))
        {
            child.Text = value;
        }
    }

    private void PlayPopup(GComponent demo)
    {
        popupMenu ??= new PopupMenu(global::Basics.PopupMenu.URL);
        if (popupMenu.ItemCount == 0)
        {
            popupMenu.AddItem("Item 1", () => Game.Logger.LogInformation("[FGUI][POPUP][MENU] click Item 1"));
            popupMenu.AddItem("Item 2", () => Game.Logger.LogInformation("[FGUI][POPUP][MENU] click Item 2"));
            popupMenu.AddItem("Item 3", () => Game.Logger.LogInformation("[FGUI][POPUP][MENU] click Item 3"));
            popupMenu.AddItem("Item 4", () => Game.Logger.LogInformation("[FGUI][POPUP][MENU] click Item 4"));
        }

        popupComponent ??= CreateObjectByCandidates(
            "Basics",
            "Component12",
            "Component12.xml",
            "components/Component12",
            "components/Component12.xml");
        popupComponent ??= UIPackage.CreateObjectFromURL("ui://Basics/Component12") as GComponent;

        Game.Logger.LogInformation(
            "[FGUI][POPUP][BIND] demo={Demo} popupMenuItems={Items} popupAnyCreated={PopupAny}",
            demo.PackageItem?.Name ?? demo.Name,
            popupMenu.ItemCount,
            popupComponent != null);

        var n0 = demo.GetChild("n0") ?? FindChildRecursive(demo, "n0");
        var n1 = demo.GetChild("n1") ?? FindChildRecursive(demo, "n1");
        Game.Logger.LogInformation(
            "[FGUI][POPUP][BIND-RESOLVE] n0={N0Name}/{N0Type}/parent={N0Parent} n1={N1Name}/{N1Type}/parent={N1Parent}",
            n0?.Name ?? "<null>",
            n0?.GetType().Name ?? "<null>",
            n0?.Parent?.Name ?? "<none>",
            n1?.Name ?? "<null>",
            n1?.GetType().Name ?? "<null>",
            n1?.Parent?.Name ?? "<none>");
        ResetPopupClickHandlersRecursive(n0);
        ResetPopupClickHandlersRecursive(n1);
        BindPopupTrigger(
            n0,
            "n0",
            "menu",
            () =>
            {
                var nowMs = Environment.TickCount64;
                var deltaMs = nowMs - popupMenuLastToggleTickMs;
                if (deltaMs >= 0 && deltaMs < PopupMenuDuplicateClickSuppressMs)
                {
                    Game.Logger.LogWarning(
                        "[FGUI][POPUP][TRACE][MENU] toggle-skip reason=duplicate-click deltaMs={Delta}",
                        deltaMs);
                    return;
                }

                if (popupMenuToggleInProgress)
                {
                    Game.Logger.LogWarning(
                        "[FGUI][POPUP][TRACE][MENU] toggle-skip reason=in-progress");
                    return;
                }

                popupMenuLastToggleTickMs = nowMs;
                popupMenuToggleInProgress = true;
                try
                {
                var anchor = n0 ?? demo;
                var pane = popupMenu.ContentPane;
                var isOpen = pane != null
                    && pane.Parent != null
                    && pane.Visible
                    && pane.FinalVisible
                    && pane.Alpha > 0.001f
                    && pane.Width > 1f
                    && pane.Height > 1f;
                Game.Logger.LogWarning(
                    "[FGUI][POPUP][TRACE][MENU] state n0 shown={Shown} paneParent={PaneParent} paneVisible={PaneVisible} paneFinal={PaneFinal} paneAlpha={PaneAlpha} paneSize={PaneW}x{PaneH} isOpen={IsOpen}",
                    popupMenu.IsShown,
                    pane?.Parent?.Name ?? "<none>",
                    pane?.Visible ?? false,
                    pane?.FinalVisible ?? false,
                    pane?.Alpha ?? 0f,
                    pane?.Width ?? 0f,
                    pane?.Height ?? 0f,
                    isOpen);

                if (isOpen)
                {
                    Game.Logger.LogWarning("[FGUI][POPUP][TRACE][MENU] toggle-close n0");
                    popupMenu.Hide();
                    return;
                }

                Game.Logger.LogWarning("[FGUI][POPUP][TRACE][MENU] toggle-open n0");
                popupMenu.Show(anchor, PopupDirection.Down);
                var paneAfterShow = popupMenu.ContentPane;
                ForcePopupMenuPaneOnHost(anchor, paneAfterShow, PopupDirection.Down);
                if (paneAfterShow != null)
                {
                    Game.Logger.LogInformation(
                        "[FGUI][POPUP][MENU-POST] pane={Pane} parent={Parent} visible={Visible} final={Final} alpha={Alpha} xy={X},{Y} size={W}x{H} paneNative={PaneNative} parentNative={ParentNative}",
                        paneAfterShow.Name ?? "<unnamed>",
                        paneAfterShow.Parent?.Name ?? "<none>",
                        paneAfterShow.Visible,
                        paneAfterShow.FinalVisible,
                        paneAfterShow.Alpha,
                        paneAfterShow.X,
                        paneAfterShow.Y,
                        paneAfterShow.Width,
                        paneAfterShow.Height,
                        paneAfterShow.NativeObject != null,
                        paneAfterShow.Parent?.NativeObject != null);
                }
                }
                finally
                {
                    popupMenuToggleInProgress = false;
                }
            });

        if (popupComponent != null)
        {
            // n1 follows original popup-any toggle semantics.
            // Bind only the root button to avoid one physical click being dispatched twice via nested children.
            BindPopupTrigger(n1, "n1", "any", () => TogglePopupComponentOnHost(n1 ?? demo, popupComponent));

            // Make popup-any content interactive in SCE touch bridge:
            // bind popup root click so child image clicks relay to popup root and can close it.
            BindPopupTrigger(popupComponent, "popupAny", "popup-close", HidePopupComponentOverlay);
        }
        else
        {
            Game.Logger.LogWarning("[FGUI][POPUP][BIND] skip n1 because popupComponent create failed");
        }

        demo.OnRightClick.Add(_ => popupMenu.Show(demo, PopupDirection.Down));
    }

    private void BindPopupTrigger(GObject? trigger, string triggerName, string route, Action onClick)
    {
        if (trigger == null)
        {
            Game.Logger.LogWarning("[FGUI][POPUP][BIND] target={Target} missing route={Route}", triggerName, route);
            return;
        }

        Game.Logger.LogInformation(
            "[FGUI][POPUP][BIND] target={Target} route={Route} type={Type} touchable={Touchable} visible={Visible} final={Final}",
            triggerName,
            route,
            trigger.GetType().Name,
            trigger.Touchable,
            trigger.Visible,
            trigger.FinalVisible);

        var callbackKey = $"{trigger.Id}|{route}";
        if (popupTriggerClickCallbacks.TryGetValue(callbackKey, out var oldCallback))
        {
            trigger.OnClick.Remove(oldCallback);
        }

        EventCallback callback = _ =>
        {
            try
            {
                Game.Logger.LogWarning(
                    "[FGUI][POPUP][TRACE][CLICK-IN] target={Target} route={Route} name={Name} type={Type} parent={Parent} native={Native} touchable={Touchable} visible={Visible} final={Final}",
                    triggerName,
                    route,
                    trigger.Name,
                    trigger.GetType().Name,
                    trigger.Parent?.Name ?? "<none>",
                    trigger.NativeObject != null,
                    trigger.Touchable,
                    trigger.Visible,
                    trigger.FinalVisible);

                Game.Logger.LogWarning(
                    "[FGUI][POPUP][TRACE][CALL] target={Target} route={Route} action=begin",
                    triggerName,
                    route);

                onClick();

                Game.Logger.LogWarning(
                    "[FGUI][POPUP][TRACE][CALL] target={Target} route={Route} action=end",
                    triggerName,
                    route);
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(
                    ex,
                    "[FGUI][POPUP][TRACE][CALL] target={Target} route={Route} action=error",
                    triggerName,
                    route);
            }
        };
        popupTriggerClickCallbacks[callbackKey] = callback;
        trigger.OnClick.Add(callback);

        EnsureTouchBindingRecursive(trigger);
    }

    private void BindPopupTriggerChildren(GObject? trigger, string triggerName, string route, Action onClick)
    {
        if (trigger is not GComponent component)
        {
            return;
        }

        for (var i = 0; i < component.NumChildren; i++)
        {
            var child = component.GetChildAt(i);
            BindPopupTrigger(child, $"{triggerName}/{child.Name}", route, onClick);
            BindPopupTriggerChildren(child, $"{triggerName}/{child.Name}", route, onClick);
        }
    }

    private static void ResetPopupClickHandlersRecursive(GObject? node)
    {
        if (node == null)
        {
            return;
        }

        node.RemoveEventListeners("onClick");
        if (node is not GComponent component)
        {
            return;
        }

        for (var i = 0; i < component.NumChildren; i++)
        {
            ResetPopupClickHandlersRecursive(component.GetChildAt(i));
        }
    }

    private static void EnsureTouchBindingRecursive(GObject? node)
    {
        if (node == null)
        {
            return;
        }

        FairyGUI.Render.SCERenderContext.Instance.EnsureTouchBinding(node);
        if (node is not GComponent component)
        {
            return;
        }

        for (var i = 0; i < component.NumChildren; i++)
        {
            EnsureTouchBindingRecursive(component.GetChildAt(i));
        }
    }

    private static void PlayTransition(GComponent demo)
    {
        if (FindChildRecursive(demo, "n2") is GComponent n2)
        {
            n2.GetTransition("t0")?.Play(times: int.MaxValue);
        }

        if (FindChildRecursive(demo, "n3") is GComponent n3)
        {
            n3.GetTransition("peng")?.Play(times: int.MaxValue);
        }
    }

    private void PlayWindow(GComponent demo)
    {
        Game.Logger.LogInformation(
            "[FGUI][WINDOW][BIND] demo={Demo} children={Children}",
            demo.PackageItem?.Name ?? demo.Name,
            demo.NumChildren);

        var n0Resolved = demo.GetChild("n0") ?? FindChildRecursive(demo, "n0");
        var n1Resolved = demo.GetChild("n1") ?? FindChildRecursive(demo, "n1");
        Game.Logger.LogInformation(
            "[FGUI][WINDOW][BIND-RESOLVE] n0={N0Name}/{N0Type}/parent={N0Parent} n1={N1Name}/{N1Type}/parent={N1Parent}",
            n0Resolved?.Name ?? "<null>",
            n0Resolved?.GetType().Name ?? "<null>",
            n0Resolved?.Parent?.Name ?? "<none>",
            n1Resolved?.Name ?? "<null>",
            n1Resolved?.GetType().Name ?? "<null>",
            n1Resolved?.Parent?.Name ?? "<none>");

        if (n0Resolved is GObject n0)
        {
            Game.Logger.LogInformation(
                "[FGUI][WINDOW][BIND] target=n0 type={Type} touchable={Touchable} visible={Visible} final={Final}",
                n0.GetType().Name,
                n0.Touchable,
                n0.Visible,
                n0.FinalVisible);

            n0.OnClick.Add(_ =>
            {
                Game.Logger.LogInformation(
                    "[FGUI][WINDOW][CLICK] target=n0 fired touchable={Touchable} visible={Visible} final={Final}",
                    n0.Touchable,
                    n0.Visible,
                    n0.FinalVisible);

                if (windowAComponent == null || windowAComponent.Disposed)
                {
                    windowAComponent = UIPackage.CreateObjectFromURL(global::Basics.WindowA.URL) as GComponent
                        ?? CreateObjectByCandidates("Basics", "WindowA", "WindowA.xml", "Window/WindowA", "Window/WindowA.xml");
                }

                var winA = windowAComponent;
                if (winA != null)
                {
                    BindWindowCloseButton(winA, "WindowA");
                    PopulateWindowAList(winA);
                    ShowWindowOnHost(n0, winA);
                }
                else
                {
                    Game.Logger.LogWarning("[FGUI][WINDOW][CREATE] target=n0 create WindowA failed");
                }
            });
        }
        else
        {
            Game.Logger.LogWarning("[FGUI][WINDOW][BIND] target=n0 missing");
        }

        if (n1Resolved is GObject n1)
        {
            Game.Logger.LogInformation(
                "[FGUI][WINDOW][BIND] target=n1 type={Type} touchable={Touchable} visible={Visible} final={Final}",
                n1.GetType().Name,
                n1.Touchable,
                n1.Visible,
                n1.FinalVisible);

            n1.OnClick.Add(_ =>
            {
                Game.Logger.LogInformation(
                    "[FGUI][WINDOW][CLICK] target=n1 fired touchable={Touchable} visible={Visible} final={Final}",
                    n1.Touchable,
                    n1.Visible,
                    n1.FinalVisible);

                if (windowBComponent == null || windowBComponent.Disposed)
                {
                    windowBComponent = UIPackage.CreateObjectFromURL(global::Basics.WindowB.URL) as GComponent
                        ?? CreateObjectByCandidates("Basics", "WindowB", "WindowB.xml", "Window/WindowB", "Window/WindowB.xml");
                }

                var winB = windowBComponent;
                if (winB != null)
                {
                    BindWindowCloseButton(winB, "WindowB");
                    ShowWindowOnHost(n1, winB);
                    winB.GetTransition("t1")?.Play();
                }
                else
                {
                    Game.Logger.LogWarning("[FGUI][WINDOW][CREATE] target=n1 create WindowB failed");
                }
            });
        }
        else
        {
            Game.Logger.LogWarning("[FGUI][WINDOW][BIND] target=n1 missing");
        }
    }

    private void HidePopupComponentOverlay()
    {
        if (popupComponent == null)
        {
            return;
        }

        popupComponent.Visible = false;
        if (popupComponent.Parent != null)
        {
            popupComponent.RemoveFromParent();
        }
    }

    private void ShowPopupComponentCenteredOnHost(GObject anchor, GComponent popup)
    {
        var host = runtimeDemoHost ?? ResolveHostFromAnchor(anchor) ?? main;
        if (host == null)
        {
            Game.Logger.LogWarning("[FGUI][POPUP][ANY] host missing anchor={Anchor}", anchor.Name);
            return;
        }

        var alreadyShown = popup.Parent == host
            && popup.Visible
            && popup.FinalVisible
            && popup.Alpha > 0.001f
            && popup.Width > 1f
            && popup.Height > 1f;
        if (popup.Parent != host)
        {
            popup.RemoveFromParent();
            host.AddChild(popup);
        }
        else
        {
            host.SetChildIndex(popup, host.NumChildren - 1);
        }

        if (!alreadyShown)
        {
            var x = (host.Width - popup.Width) * 0.5f;
            var y = (host.Height - popup.Height) * 0.5f;
            var maxX = Math.Max(0f, host.Width - popup.Width);
            var maxY = Math.Max(0f, host.Height - popup.Height);
            popup.SetXY(Math.Clamp(x, 0f, maxX), Math.Clamp(y, 0f, maxY));
        }

        popup.Touchable = true;
        popup.Alpha = 1f;
        popup.Visible = true;
        host.SetChildIndex(popup, host.NumChildren - 1);
        FairyGUI.Render.SCERenderContext.Instance.RenderChild(host, popup);
        EnsureTouchBindingRecursive(popup);
        Game.Logger.LogInformation(
            "[FGUI][POPUP][ANY] show-center anchor={Anchor} host={Host} hostSize={HostW}x{HostH} popupSize={PopupW}x{PopupH} xy={X},{Y} alpha={Alpha} visible={Visible} final={Final} native={Native} alreadyShown={AlreadyShown}",
            anchor.Name,
            host.Name ?? "<unnamed>",
            host.Width,
            host.Height,
            popup.Width,
            popup.Height,
            popup.X,
            popup.Y,
            popup.Alpha,
            popup.Visible,
            popup.FinalVisible,
            popup.NativeObject != null,
            alreadyShown);
    }

    private void TogglePopupComponentOnHost(GObject anchor, GComponent popup)
    {
        var nowMs = Environment.TickCount64;
        var deltaMs = nowMs - popupAnyLastToggleTickMs;
        if (deltaMs >= 0 && deltaMs < PopupAnyDuplicateClickSuppressMs)
        {
            Game.Logger.LogWarning(
                "[FGUI][POPUP][TRACE][ANY] toggle-skip reason=duplicate-click deltaMs={Delta}",
                deltaMs);
            return;
        }

        if (popupAnyToggleInProgress)
        {
            Game.Logger.LogWarning(
                "[FGUI][POPUP][TRACE][ANY] toggle-skip reason=in-progress");
            return;
        }

        popupAnyLastToggleTickMs = nowMs;
        popupAnyToggleInProgress = true;
        try
        {
            var isOpen = popup.Parent != null
                && popup.Visible
                && popup.FinalVisible
                && popup.Alpha > 0.001f
                && popup.Width > 1f
                && popup.Height > 1f;

            Game.Logger.LogWarning(
                "[FGUI][POPUP][TRACE][ANY] state anchor={Anchor} parent={Parent} visible={Visible} final={Final} alpha={Alpha} size={W}x{H} isOpen={IsOpen}",
                anchor.Name,
                popup.Parent?.Name ?? "<none>",
                popup.Visible,
                popup.FinalVisible,
                popup.Alpha,
                popup.Width,
                popup.Height,
                isOpen);

            if (isOpen)
            {
                Game.Logger.LogWarning("[FGUI][POPUP][TRACE][ANY] toggle-close anchor={Anchor}", anchor.Name);
                HidePopupComponentOverlay();
                return;
            }

            Game.Logger.LogWarning("[FGUI][POPUP][TRACE][ANY] toggle-open anchor={Anchor}", anchor.Name);
            ShowPopupComponentCenteredOnHost(anchor, popup);
        }
        finally
        {
            popupAnyToggleInProgress = false;
        }
    }

    private void ForcePopupMenuPaneOnHost(GObject anchor, GComponent? pane, PopupDirection direction)
    {
        if (pane == null)
        {
            Game.Logger.LogWarning("[FGUI][POPUP][FORCE] pane missing anchor={Anchor}", anchor.Name);
            return;
        }

        var host = runtimeDemoHost ?? ResolveHostFromAnchor(anchor) ?? main;
        if (host == null)
        {
            Game.Logger.LogWarning("[FGUI][POPUP][FORCE] host missing anchor={Anchor}", anchor.Name);
            return;
        }

        if (pane.Parent != host)
        {
            pane.RemoveFromParent();
            host.AddChild(pane);
        }
        else
        {
            host.SetChildIndex(pane, host.NumChildren - 1);
        }

        var anchorPos = ResolvePositionRelativeToHost(anchor, host);
        var x = anchorPos.X;
        var y = anchorPos.Y + anchor.Height;
        if (direction == PopupDirection.Up)
        {
            y = anchorPos.Y - pane.Height;
        }
        else if (direction == PopupDirection.Auto && y + pane.Height > host.Height)
        {
            y = anchorPos.Y - pane.Height;
        }

        var maxX = Math.Max(0f, host.Width - pane.Width);
        var maxY = Math.Max(0f, host.Height - pane.Height);
        pane.SetXY(Math.Clamp(x, 0f, maxX), Math.Clamp(y, 0f, maxY));
        pane.Touchable = true;
        pane.Visible = true;
        host.SetChildIndex(pane, host.NumChildren - 1);
        EnsureTouchBindingRecursive(pane);

        Game.Logger.LogWarning(
            "[FGUI][POPUP][FORCE] anchor={Anchor} host={Host} hostSize={HostW}x{HostH} paneParent={Parent} paneXY={X},{Y} paneSize={PaneW}x{PaneH} visible={Visible} final={Final} touchable={Touchable}",
            anchor.Name,
            host.Name ?? "<unnamed>",
            host.Width,
            host.Height,
            pane.Parent?.Name ?? "<none>",
            pane.X,
            pane.Y,
            pane.Width,
            pane.Height,
            pane.Visible,
            pane.FinalVisible,
            pane.Touchable);
    }

    private void ShowWindowOnHost(GObject anchor, GComponent window)
    {
        var host = runtimeDemoHost ?? ResolveHostFromAnchor(anchor) ?? main;
        if (host == null)
        {
            Game.Logger.LogWarning("[FGUI][WINDOW][SHOW] host missing target={Target} window={Window}", anchor.Name, window.Name);
            return;
        }

        if (window.Parent != host)
        {
            window.RemoveFromParent();
            host.AddChild(window);
        }
        else
        {
            host.SetChildIndex(window, host.NumChildren - 1);
        }

        var x = (host.Width - window.Width) * 0.5f;
        var y = (host.Height - window.Height) * 0.5f;
        var maxX = Math.Max(0f, host.Width - window.Width);
        var maxY = Math.Max(0f, host.Height - window.Height);
        window.SetXY(Math.Clamp(x, 0f, maxX), Math.Clamp(y, 0f, maxY));
        window.Touchable = true;
        window.Visible = true;
        FairyGUI.Render.SCERenderContext.Instance.RenderChild(host, window);
        EnsureTouchBindingRecursive(window);
        Game.Logger.LogInformation(
            "[FGUI][WINDOW][SHOW] target={Target} host={Host} hostSize={HostW}x{HostH} window={Window} windowSize={WinW}x{WinH} xy={X},{Y} final={Final} visible={Visible} touchable={Touchable} native={Native}",
            anchor.Name,
            host.Name ?? "<unnamed>",
            host.Width,
            host.Height,
            window.PackageItem?.Name ?? window.Name,
            window.Width,
            window.Height,
            window.X,
            window.Y,
            window.FinalVisible,
            window.Visible,
            window.Touchable,
            window.NativeObject != null);
    }

    private void BindWindowCloseButton(GComponent window, string windowTag)
    {
        var closeButton = FindChildRecursive(window, "closeButton");
        if (closeButton == null)
        {
            Game.Logger.LogWarning("[FGUI][WINDOW][CLOSE-BIND] missing closeButton window={Window}", windowTag);
            return;
        }

        var key = $"{window.Id}|{closeButton.Id}";
        if (!windowCloseBindingKeys.Add(key))
        {
            return;
        }

        closeButton.OnClick.Add(_ =>
        {
            Game.Logger.LogInformation(
                "[FGUI][WINDOW][CLOSE] window={Window} closeButton={CloseButton} parent={Parent}",
            windowTag,
            closeButton.Name,
            window.Parent?.Name ?? "<none>");
            window.GetTransition("t1")?.Stop();
            window.Visible = false;
            window.Touchable = false;
            if (window.Parent != null)
            {
                window.RemoveFromParent();
            }
            FairyGUI.Render.SCERenderContext.Instance.RemoveFromParent(window);
        });
        EnsureTouchBindingRecursive(closeButton);
        Game.Logger.LogInformation(
            "[FGUI][WINDOW][CLOSE-BIND] window={Window} closeButtonType={Type} touchable={Touchable}",
            windowTag,
            closeButton.GetType().Name,
            closeButton.Touchable);
    }

    private static void PopulateWindowAList(GComponent windowA)
    {
        if (FindChildRecursive(windowA, "n6") is not GList list)
        {
            return;
        }

        list.RemoveChildrenToPool();
        var iconUrl = UIRuntime.GetItemURL("Basics", "r4");
        for (var i = 0; i < 6; i++)
        {
            if (list.AddItemFromPool() is not GButton item)
            {
                continue;
            }

            item.Title = i.ToString();
            if (!string.IsNullOrEmpty(iconUrl))
            {
                item.Icon = iconUrl;
            }
        }
    }

    private static void DisposeWindowComponent(ref GComponent? window)
    {
        if (window == null)
        {
            return;
        }

        if (window.Parent != null)
        {
            window.RemoveFromParent();
        }

        if (!window.Disposed)
        {
            window.Dispose();
        }

        window = null;
    }

    private static GComponent? ResolveHostFromAnchor(GObject? anchor)
    {
        GComponent? host = null;
        GObject? cursor = anchor?.Parent;
        while (cursor != null)
        {
            if (cursor is GComponent component)
            {
                host = component;
            }

            cursor = cursor.Parent;
        }

        return host;
    }

    private static PointF ResolvePositionRelativeToHost(GObject target, GComponent host)
    {
        var x = target.X;
        var y = target.Y;
        GObject? cursor = target.Parent;
        while (cursor != null && cursor != host)
        {
            x += cursor.X;
            y += cursor.Y;
            cursor = cursor.Parent;
        }

        return cursor == host ? new PointF(x, y) : new PointF(target.X, target.Y);
    }

    private static GComponent? CreateObjectByCandidates(string packageName, params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (UIPackage.CreateObject(packageName, name) is GComponent component)
            {
                return component;
            }
        }

        return null;
    }

    private static void PlayDragDrop(GComponent demo)
    {
        static PointF GetAbsolutePosition(GObject obj)
        {
            var x = obj.X;
            var y = obj.Y;
            GObject? cursor = obj.Parent;
            while (cursor != null)
            {
                x += cursor.X;
                y += cursor.Y;
                cursor = cursor.Parent;
            }

            return new PointF(x, y);
        }

        static RectangleF GetAbsoluteRect(GObject obj)
        {
            var abs = GetAbsolutePosition(obj);
            return new RectangleF(abs.X, abs.Y, obj.Width, obj.Height);
        }

        static bool Intersects(RectangleF a, RectangleF b)
        {
            return a.Left < b.Right &&
                   a.Right > b.Left &&
                   a.Top < b.Bottom &&
                   a.Bottom > b.Top;
        }

        static bool ContainsLogicalPoint(GObject obj, PointF logicalPoint)
        {
            var abs = GetAbsolutePosition(obj);
            return logicalPoint.X >= abs.X &&
                   logicalPoint.X <= abs.X + obj.Width &&
                   logicalPoint.Y >= abs.Y &&
                   logicalPoint.Y <= abs.Y + obj.Height;
        }

        var cTarget = FindChildRecursive(demo, "c") as GButton;
        if (FindChildRecursive(demo, "a") is GObject a)
        {
            a.Draggable = true;
        }

        if (FindChildRecursive(demo, "b") is GButton b)
        {
            b.Draggable = true;
            bool dragFinalized = false;

            void FinalizeDrag()
            {
                if (dragFinalized || !DragDropManager.IsDragging)
                {
                    return;
                }

                dragFinalized = true;
                GObject? dropTarget = null;
                if (cTarget != null)
                {
                    var touchPoint = DragDropManager.LastLogicalTouchPoint;
                    var pointerOnC = ContainsLogicalPoint(cTarget, touchPoint);
                    var agentOverlapsC = false;
                    if (!pointerOnC && DragDropManager.DragAgent != null)
                    {
                        agentOverlapsC = Intersects(GetAbsoluteRect(cTarget), GetAbsoluteRect(DragDropManager.DragAgent));
                    }

                    if (pointerOnC || agentOverlapsC)
                    {
                        dropTarget = cTarget;
                    }
                }

                DragDropManager.OnDragEnd(DragDropManager.LastTouchPoint, dropTarget);
            }

            b.OnDragStart.Add(ctx =>
            {
                ctx.PreventDefault();
                dragFinalized = false;
                DragDropManager.StartDrag(b, b.Icon, b.Icon);
                b.StartDrag();
            });

            b.OnDragEnd.Add(_ =>
            {
                FinalizeDrag();
            });

            b.OnTouchEnd.Add(_ => FinalizeDrag());
        }

        if (cTarget is GButton c)
        {
            c.Icon = null;
            c.OnDrop.Add(ctx =>
            {
                if (ctx.Data is DropEventData drop && drop.SourceData is string icon)
                {
                    c.Icon = icon;
                    Game.Logger.LogInformation(
                        "[FGUI][DragDrop] onDrop target=c source={Source} icon={Icon}",
                        drop.Source?.Name ?? "<none>",
                        icon);
                }
            });
        }

        var bounds = FindChildRecursive(demo, "n7");
        if (bounds is GGraph boundsGraph && boundsGraph.LineSize > 0 && boundsGraph.FillColor.A == 0)
        {
            // SCE currently does not render GGraph rect stroke-only outlines.
            // Apply a light fill fallback so bounds remains visible.
            var line = boundsGraph.LineColor;
            boundsGraph.FillColor = Color.FromArgb(28, line.R, line.G, line.B);
            Game.Logger.LogInformation(
                "[FGUI][DragDrop] n7 stroke-only graph fallback fill applied alpha={Alpha}",
                boundsGraph.FillColor.A);
        }

        if (FindChildRecursive(demo, "d") is GObject d)
        {
            d.Draggable = true;
            if (bounds != null)
            {
                d.OnDragMove.Add(_ =>
                {
                    var minX = bounds.X;
                    var minY = bounds.Y;
                    var maxX = Math.Max(minX, bounds.X + bounds.Width - d.Width);
                    var maxY = Math.Max(minY, bounds.Y + bounds.Height - d.Height);
                    var clampedX = Math.Clamp(d.X, minX, maxX);
                    var clampedY = Math.Clamp(d.Y, minY, maxY);
                    if (Math.Abs(clampedX - d.X) > 0.001f || Math.Abs(clampedY - d.Y) > 0.001f)
                    {
                        d.SetXY(clampedX, clampedY);
                    }
                });
            }
        }
    }

    private static void PlayController(GComponent demo)
    {
        if (demo is not Basics.Demo_Controller typed)
        {
            return;
        }

        typed.c1.SelectedIndex = 0;
        typed.c2.SelectedIndex = 0;
    }

    private static void PlayGraph(GComponent demo)
    {
        // Keep default published data from FGUI editor.
        // Do not hardcode Demo_Graph runtime geometry here.
    }

    private void PlayDepth(GComponent demo)
    {
        if (FindChildRecursive(demo, "n22") is not GComponent testContainer)
        {
            return;
        }

        var fixedObj = FindChildRecursive(testContainer, "n0");
        if (fixedObj != null)
        {
            fixedObj.SortingOrder = 100;
            fixedObj.Draggable = true;
            depthStartPos = new PointF(fixedObj.X, fixedObj.Y);
        }

        if (FindChildRecursive(demo, "btn0") is GObject btn0)
        {
            btn0.OnClick.Add(_ =>
            {
                var graph = new GGraph();
                depthStartPos = new PointF(depthStartPos.X + 10, depthStartPos.Y + 10);
                graph.SetXY(depthStartPos.X, depthStartPos.Y);
                graph.DrawRect(150, 150, 1, Color.Black, Color.Red);
                testContainer.AddChild(graph);
            });
        }

        if (FindChildRecursive(demo, "btn1") is GObject btn1)
        {
            btn1.OnClick.Add(_ =>
            {
                var graph = new GGraph();
                depthStartPos = new PointF(depthStartPos.X + 10, depthStartPos.Y + 10);
                graph.SetXY(depthStartPos.X, depthStartPos.Y);
                graph.DrawRect(150, 150, 1, Color.Black, Color.Green);
                graph.SortingOrder = 200;
                testContainer.AddChild(graph);
            });
        }
    }

    private static void PlayProgressBar(GComponent demo)
    {
        var progressBars = EnumerateChildrenRecursive(demo)
            .OfType<GProgressBar>()
            .ToList();
        if (progressBars.Count == 0)
        {
            return;
        }

        var target = $"progress::{demo.GetHashCode()}";
        GTween.Kill(target);
        GTween.To(0f, 100f, 1f)
            .SetTarget(target)
            .SetEase(EaseType.Linear)
            .SetRepeat(-1, false)
            .OnUpdate(t =>
            {
                foreach (var bar in progressBars)
                {
                    bar.Value = t.Value.X;
                }
            });
    }
}

#endif



