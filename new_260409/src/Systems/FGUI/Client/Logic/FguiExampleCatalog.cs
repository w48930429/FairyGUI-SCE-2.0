#if CLIENT
namespace GameEntry;

internal sealed class FguiExampleCatalogEntry
{
    public FguiExampleCatalogEntry(string packageName, params string[] componentCandidates)
    {
        PackageName = packageName;
        ComponentCandidates = componentCandidates.Length == 0 ? ["Main"] : componentCandidates;
    }

    public string PackageName { get; }
    public string PackagePath => $"ui/image/fgui/scatter/{PackageName}/{PackageName}";
    public IReadOnlyList<string> ComponentCandidates { get; }
}

internal static class FguiExampleCatalog
{
    // Based on FairyGUI Unity examples under Assets/Examples.
    public static IReadOnlyList<FguiExampleCatalogEntry> Entries { get; } =
    [
        new("Bag", "BagWin", "Main"),
        new("Basics", "Main", "Component12"),
        new("BundleUsage", "Main"),
        new("Cooldown", "Main"),
        new("Curve", "Main"),
        new("CutScene", "Main"),
        new("EmitNumbers", "Main"),
        new("Emoji", "Main", "EmojiSelectUI", "EmojiSelectUI_ios"),
        new("Extension", "Main"),
        new("Filter", "Main"),
        new("Gesture", "Main"),
        new("Guide", "Main", "GuideLayer"),
        new("HeadBar", "Main"),
        new("HitTest", "Main"),
        new("Joystick", "Main"),
        new("LoopList", "Main"),
        new("ModalWaiting", "Main"),
        new("Model", "Main"),
        new("Particles", "Main"),
        new("Perspective", "Main"),
        new("PullToRefresh", "Main"),
        new("RenderTexture", "Main"),
        new("ScrollPane", "Main"),
        new("TextMeshPro", "Main"),
        new("Transition", "Main"),
        new("TreeView", "Main"),
        new("TurnCard", "Main"),
        new("TurnPage", "Main"),
        new("TypingEffect", "Main"),
        new("VirtualList", "Main"),
    ];
}
#endif
