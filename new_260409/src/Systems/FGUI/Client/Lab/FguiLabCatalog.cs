#if CLIENT
namespace GameEntry;

internal sealed partial class FguiLabCatalogEntry
{
    public FguiLabCatalogEntry(string packageName, params string[] componentCandidates)
    {
        PackageName = packageName;
        ComponentCandidates = componentCandidates.Length == 0 ? ["Main"] : componentCandidates;
    }

    public string PackageName { get; }
    public IReadOnlyList<string> ComponentCandidates { get; }
    public string PackagePath => $"ui/image/fgui/scatter/{PackageName}/{PackageName}";
}

internal static partial class FguiLabCatalog
{
    // First batch: locked set + Basics baseline logic package.
    public static IReadOnlyList<FguiLabCatalogEntry> Entries { get; } =
    [
        new("Basics", "Main"),
        new("Cooldown", "Main"),
        new("Extension", "Main"),
        new("Filter", "Main"),
        new("Guide", "Main"),
        new("Joystick", "Main"),
        new("TurnCard", "Main"),
        new("Transition", "Main"),
        new("Emoji", "Main"),
        new("LoopList", "Main"),
        new("VirtualList", "Main"),
        new("ScrollPane", "Main"),
        new("ModalWaiting", "Main"),
        new("TypingEffect", "Main"),
    ];
}
#endif


