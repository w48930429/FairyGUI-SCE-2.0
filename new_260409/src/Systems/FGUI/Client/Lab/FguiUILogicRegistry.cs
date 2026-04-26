#if CLIENT
namespace GameEntry;

internal static partial class FguiUILogicRegistry
{
    private static readonly Dictionary<string, Func<IFguiUILogic>> UILogicFactories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Basics"] = static () => new BasicsUILogic(),
            ["Cooldown"] = static () => new CooldownUILogic(),
            ["Extension"] = static () => new ExtensionUILogic(),
            ["Filter"] = static () => new FilterUILogic(),
            ["Guide"] = static () => new GuideUILogic(),
            ["Joystick"] = static () => new JoystickUILogic(),
            ["TurnCard"] = static () => new TurnCardUILogic(),
            ["Transition"] = static () => new TransitionUILogic(),
            ["Emoji"] = static () => new EmojiUILogic(),
            ["LoopList"] = static () => new LoopListUILogic(),
            ["VirtualList"] = static () => new VirtualListUILogic(),
            ["TreeView"] = static () => new TreeViewUILogic(),
            ["ScrollPane"] = static () => new ScrollPaneUILogic(),
            ["ModalWaiting"] = static () => new ModalWaitingUILogic(),
            ["TypingEffect"] = static () => new TypingEffectUILogic(),
        };

    private static readonly HashSet<string> BoundPackages = new(StringComparer.OrdinalIgnoreCase);

    public static void PreparePackageBindings(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName) || BoundPackages.Contains(packageName))
        {
            return;
        }

        switch (packageName.ToLowerInvariant())
        {
            case "bag":
                Bag.BagBinder.BindAll();
                break;
            case "basics":
                Basics.BasicsBinder.BindAll();
                break;
            case "cooldown":
                Cooldown.CooldownBinder.BindAll();
                break;
            case "curve":
                Curve.CurveBinder.BindAll();
                break;
            case "gesture":
                Gesture.GestureBinder.BindAll();
                break;
            case "joystick":
                Joystick.JoystickBinder.BindAll();
                break;
            case "looplist":
                LoopList.LoopListBinder.BindAll();
                break;
            case "treeview":
                TreeView.TreeViewBinder.BindAll();
                break;
            case "turnpage":
                TurnPage.TurnPageBinder.BindAll();
                break;
            case "typingeffect":
                TypingEffect.TypingEffectBinder.BindAll();
                break;
            case "virtuallist":
                VirtualList.VirtualListBinder.BindAll();
                break;
            default:
                return;
        }

        BoundPackages.Add(packageName);
    }

    public static IFguiUILogic? Create(string packageName)
    {
        return UILogicFactories.TryGetValue(packageName, out var factory) ? factory() : null;
    }
}
#endif




