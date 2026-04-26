#if CLIENT
namespace GameEntry;

public sealed class FguiBottomBasicEntryClientSys : IGameClass
{
    private static bool initialized;
    private static Panel? root;
    private static Button? basicButton;

    public static void OnRegisterGameClass()
    {
        Game.OnGameUIInitialization += OnGameUiInitialization;
        Game.OnGameStart += OnGameStart;
    }

    private static void OnGameUiInitialization()
    {
        EnsureBottomEntry("OnGameUIInitialization");
    }

    private static void OnGameStart()
    {
        EnsureBottomEntry("OnGameStart");
    }

    private static void EnsureBottomEntry(string source)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        root = new Panel
        {
            Width = 0f,
            Height = 0f,
            WidthStretchRatio = 1f,
            HeightStretchRatio = 1f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
        };
        _ = root.AddToVisualTree();

        basicButton = new Button
        {
            Parent = root,
            Width = 132f,
            Height = 38f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(12f, 0f, 0f, 12f),
            CornerRadius = 8f,
            Background = new SolidColorBrush(Color.FromArgb(220, 36, 118, 196)),
        };

        _ = new Label
        {
            Parent = basicButton,
            Text = "Basic",
            FontSize = 14f,
            Bold = true,
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

        basicButton.OnPointerClicked += (_, _) =>
        {
            var shown = FguiExampleRunnerClientSys.ShowPackage("Basics", forceReloadPackage: true);
            Game.Logger.LogWarning("[FGUI][BOTTOM] click Basic shown={Shown}", shown);
        };

        Game.Logger.LogWarning("[FGUI][BOTTOM] Basic entry ready source={Source}", source);
    }
}
#endif
