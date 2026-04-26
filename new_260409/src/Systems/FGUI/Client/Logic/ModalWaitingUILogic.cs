#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class ModalWaitingUILogic : FguiUILogicBase
{
    private static readonly object GlobalWaitToken = new();
    private static readonly ModalWaitingTestWindow TestWindow = new();
    private static GGraph? GlobalWaitPane;

    private GObject? openWindowButton;

    public override string PackageName => "ModalWaiting";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        openWindowButton = FindChildRecursive(view, "n0");
        if (openWindowButton == null)
        {
            message = "bind failed: missing n0";
            return false;
        }

        openWindowButton.OnClick.Add(OnClickOpenWindow);
        ShowGlobalWaitOnce();
        message = "modal waiting logic bound";
        return true;
    }

    public override bool RunSmoke(out string message)
    {
        ShowGlobalWaitOnce();
        message = "modal waiting smoke: global wait shown";
        return true;
    }

    public override void Cleanup()
    {
        if (openWindowButton != null)
        {
            openWindowButton.OnClick.Remove(OnClickOpenWindow);
        }

        GTween.Kill(GlobalWaitToken);
        CloseGlobalWait();

        openWindowButton = null;
        Root = null;
    }

    private static void ShowGlobalWaitOnce()
    {
        GTween.Kill(GlobalWaitToken);
        ShowGlobalWait();
        GTween.To(0f, 1f, 3f)
            .SetTarget(GlobalWaitToken)
            .SetEase(EaseType.Linear)
            .OnComplete(_ => CloseGlobalWait());
    }

    private static void OnClickOpenWindow(EventContext _)
    {
        TestWindow.Show();
    }

    private sealed class ModalWaitingTestWindow : Window
    {
        private readonly object localWaitToken = new();
        private bool closeBound;

        protected override void OnInit()
        {
            ContentPane = UIPackage.CreateObject("ModalWaiting", "TestWin") as GComponent;
            var close = ContentPane?.GetChild("n1");
            if (close != null && !closeBound)
            {
                close.OnClick.Add(OnClickWait);
                closeBound = true;
            }
        }

        private void OnClickWait(EventContext _)
        {
            GTween.Kill(localWaitToken);
            ShowGlobalWait();
            GTween.To(0f, 1f, 3f)
                .SetTarget(localWaitToken)
                .SetEase(EaseType.Linear)
                .OnComplete(__ => CloseGlobalWait());
        }
    }

    private static void ShowGlobalWait()
    {
        if (GlobalWaitPane == null || GlobalWaitPane.Disposed)
        {
            GlobalWaitPane = new GGraph
            {
                Name = "modal_wait_pane",
                Touchable = true
            };
        }

        GlobalWaitPane.SetSize(UIRuntime.RootWidth, UIRuntime.RootHeight, true);
        GlobalWaitPane.DrawRect(GlobalWaitPane.Width, GlobalWaitPane.Height, 0, Color.Transparent, Color.FromArgb(100, 0, 0, 0));
        GlobalWaitPane.Visible = true;
        UIRuntime.AddToRoot(GlobalWaitPane);
    }

    private static void CloseGlobalWait()
    {
        if (GlobalWaitPane == null)
        {
            return;
        }

        GlobalWaitPane.Visible = false;
        UIRuntime.RemoveFromRoot(GlobalWaitPane, dispose: false);
    }
}
#endif
