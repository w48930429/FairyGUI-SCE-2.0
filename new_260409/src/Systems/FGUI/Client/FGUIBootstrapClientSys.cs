#if CLIENT
using FairyGUI;
using FairyGUI.Render;

namespace GameEntry;

public sealed class FGUIBootstrapClientSys : IGameClass
{
    private static bool _initialized;
    private const float LandscapeDesignWidth = 1136f;
    private const float LandscapeDesignHeight = 640f;
    private const float PortraitDesignWidth = 640f;
    private const float PortraitDesignHeight = 1136f;

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
        new Trigger<EventGameTick>(OnGameTickAsync, keepReference: true).Register(Game.Instance);
    }

    private static Task<bool> OnGameStartAsync(object sender, EventGameStart eventArgs)
    {
        EnsureInitialized();
        return Task.FromResult(true);
    }

    private static Task<bool> OnGameTickAsync(object sender, EventGameTick eventArgs)
    {
        if (!_initialized)
        {
            return Task.FromResult(true);
        }

        var deltaSeconds = Math.Max(0, eventArgs.RealTimeDeltaInMilliseconds) / 1000f;
        SCERenderContext.Instance.Tick(deltaSeconds);
        return Task.FromResult(true);
    }

    public static void EnsureInitialized(float designWidth = 0, float designHeight = 0)
    {
        if (_initialized)
        {
            return;
        }

        var adapter = new SCEAdapter();
        if (designWidth <= 0 || designHeight <= 0)
        {
            var screen = adapter.GetScreenSize();
            var isLandscape = screen.Width >= screen.Height;
            designWidth = isLandscape ? LandscapeDesignWidth : PortraitDesignWidth;
            designHeight = isLandscape ? LandscapeDesignHeight : PortraitDesignHeight;
        }

        UIRuntime.Initialize(adapter, designWidth, designHeight);
        UIRuntime.SetContentScaleFactor(designWidth, designHeight, ScreenMatchMode.Fill);
        UIRuntime.SetImageAssetMode(ImageAssetMode.ScatterOnly);
        _initialized = true;
        SetRootInputEnabled(false, "bootstrap-init");
        Game.Logger.LogInformation(
            "[FGUI] Runtime initialized. design={DesignWidth}x{DesignHeight} match={MatchMode}",
            designWidth,
            designHeight,
            ScreenMatchMode.Fill);
    }

    public static void SetRootInputEnabled(bool enabled, string reason = "")
    {
        if (!_initialized)
        {
            return;
        }

        Game.Logger.LogWarning("[FGUI][ROOT-INPUT] bypass mode=sce-stage-direct enabled={Enabled} reason={Reason}", enabled, reason);
    }

    public static GComponent? LoadAndShow(string packagePath, string packageName, string componentName)
    {
        EnsureInitialized();
        SetRootInputEnabled(true, $"load:{packageName}/{componentName}");

        var pkg = EnsurePackageLoaded(packagePath, packageName);

        if (pkg == null)
        {
            Game.Logger.LogWarning("[FGUI] Failed to load package: {PackagePath}", packagePath);
            return null;
        }

        TryApplyDesignResolutionFromPackage(pkg);

        var view = CreateComponentWithFallback(pkg, packageName, componentName);
        if (view == null)
        {
            Game.Logger.LogWarning("[FGUI] Failed to create component: {Package}/{Component}", packageName, componentName);
            return null;
        }

        UIRuntime.AddToFullScreenRoot(view);
        Game.Logger.LogInformation("[FGUI] Show component: {Package}/{Component}", packageName, componentName);
        return view;
    }

    internal static UIPackage? EnsurePackageLoaded(string packagePath, string packageName)
    {
        var pkg = UIRuntime.GetPackage(packageName);
        if (pkg != null)
        {
            return pkg;
        }

        foreach (var candidate in EnumeratePackagePathCandidates(packagePath, packageName))
        {
            var trimmed = candidate.TrimEnd('/');
            var attempts = new (string DescName, string RuntimePath)[]
            {
                ($"{trimmed}_fui", trimmed),
                ($"{trimmed}/{packageName}_fui", $"{trimmed}/{packageName}"),
            };

            foreach (var attempt in attempts)
            {
                var descData = FGUIResourceLoader.LoadBytes(attempt.DescName, ".bytes");
                if (descData == null)
                {
                    continue;
                }

                try
                {
                    pkg = UIRuntime.AddPackage(descData, attempt.RuntimePath, FGUIResourceLoader.LoadBytes);
                    if (pkg != null)
                    {
                        return pkg;
                    }
                }
                catch (Exception ex)
                {
                    Game.Logger.LogWarning(
                        ex,
                        "[FGUI] AddPackage(data) failed. packagePath={PackagePath}, descName={DescName}",
                        attempt.RuntimePath,
                        attempt.DescName);
                }
            }
        }

        return null;
    }

    internal static GComponent? CreateComponentWithFallback(UIPackage pkg, string packageName, string componentName)
    {
        var direct = UIRuntime.CreateObject(packageName, componentName) as GComponent;
        if (direct != null)
        {
            return direct;
        }

        // FairyGUI exports may prepend folder names (for example "folder/BagWin").
        foreach (var item in pkg.GetItems())
        {
            if (item.Type != PackageItemType.Component || string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            if (!item.Name.EndsWith(componentName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fallback = pkg.CreateObject(item) as GComponent;
            if (fallback == null)
            {
                continue;
            }

            Game.Logger.LogInformation("[FGUI] Component fallback matched: requested={Requested}, actual={Actual}",
                componentName, item.Name);
            return fallback;
        }

        return null;
    }

    private static IReadOnlyList<string> EnumeratePackagePathCandidates(string packagePath, string packageName)
    {
        var normalized = (packagePath ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>(12);
        static string ToScatterPath(string path)
        {
            var value = path.Replace('\\', '/').TrimEnd('/');
            const string marker = "ui/image/fgui/";
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return value;
            }

            var markerEnd = index + marker.Length;
            var start = value.Substring(0, markerEnd);
            var tail = value.Substring(markerEnd).TrimStart('/');
            if (tail.StartsWith("scatter/", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return $"{start}scatter/{tail}";
        }

        void YieldPath(string path)
        {
            var value = path.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (yielded.Add(value))
            {
                paths.Add(value);
            }
        }
        var scatterPath = ToScatterPath(normalized);
        YieldPath(scatterPath);
        YieldPath($"{scatterPath}/{packageName}");
        YieldPath(normalized);
        YieldPath($"{normalized}/{packageName}");

        var suffix = "/" + packageName;
        if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            var basePath = normalized.Substring(0, normalized.Length - suffix.Length);
            YieldPath(ToScatterPath(basePath));
            YieldPath(basePath);
        }

        return paths;
    }

    private static void TryApplyDesignResolutionFromPackage(UIPackage pkg)
    {
        if (!pkg.TryGetDesignResolution(out var designWidth, out var designHeight))
        {
            return;
        }

        if (designWidth <= 0f || designHeight <= 0f)
        {
            return;
        }

        var changed = MathF.Abs(UIRuntime.DesignResolutionX - designWidth) > 0.01f
            || MathF.Abs(UIRuntime.DesignResolutionY - designHeight) > 0.01f;
        if (!changed)
        {
            return;
        }

        UIRuntime.SetContentScaleFactor(designWidth, designHeight, ScreenMatchMode.Fill);
        Game.Logger.LogInformation(
            "[FGUI] Design resolution from package applied: {DesignWidth}x{DesignHeight} package={Package}",
            designWidth,
            designHeight,
            pkg.Name);
    }

    internal static void ApplyRootSizedLayout(GComponent view, string packageName, string componentName)
    {
        var rootWidth = MathF.Max(1f, UIRuntime.RootWidth);
        var rootHeight = MathF.Max(1f, UIRuntime.RootHeight);
        var beforeWidth = view.Width;
        var beforeHeight = view.Height;
        var beforeX = view.X;
        var beforeY = view.Y;

        // Keep adaptation semantic: root view resized to runtime root, children follow FGUI relations.
        view.SetXY(0f, 0f);
        view.SetSize(rootWidth, rootHeight, true);
        Game.Logger.LogInformation(
            "[FGUI][LAYOUT] root-size-apply package={Package} component={Component} root={RootWidth}x{RootHeight} viewBefore={BeforeWidth}x{BeforeHeight}@{BeforeX},{BeforeY} viewAfter={AfterWidth}x{AfterHeight}@{AfterX},{AfterY}",
            packageName,
            componentName,
            rootWidth,
            rootHeight,
            beforeWidth,
            beforeHeight,
            beforeX,
            beforeY,
            view.Width,
            view.Height,
            view.X,
            view.Y);
    }
}
#endif


