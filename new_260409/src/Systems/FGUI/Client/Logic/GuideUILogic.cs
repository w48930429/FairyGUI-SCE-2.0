#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class GuideUILogic : FguiUILogicBase
{
    private GComponent? guideLayer;

    public override string PackageName => "Guide";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        guideLayer = UIPackage.CreateObject("Guide", "GuideLayer") as GComponent;
        if (guideLayer == null)
        {
            message = "bind failed: GuideLayer missing";
            return false;
        }

        guideLayer.SetSize(UIRuntime.RootWidth, UIRuntime.RootHeight, true);
        guideLayer.InitRelations();
        guideLayer.Relations?.Add(view, RelationType.Size);

        var bagBtn = FindChildRecursive(view, "bagBtn");
        var trigger = FindChildRecursive(view, "n2");
        bagBtn?.OnClick.Add(_ => guideLayer.RemoveFromParent());
        trigger?.OnClick.Add(_ => ShowGuideNearBagButton(bagBtn));
        message = "guide logic bound";
        return true;
    }

    public override void Cleanup()
    {
        if (guideLayer != null)
        {
            guideLayer.RemoveFromParent();
            if (!guideLayer.Disposed)
            {
                guideLayer.Dispose();
            }
        }

        guideLayer = null;
        Root = null;
    }

    private void ShowGuideNearBagButton(GObject? bagBtn)
    {
        if (guideLayer == null || bagBtn == null)
        {
            return;
        }

        if (Root == null)
        {
            return;
        }

        if (guideLayer.Parent != Root)
        {
            guideLayer.RemoveFromParent();
            Root.AddChild(guideLayer);
        }
        else
        {
            Root.SetChildIndex(guideLayer, Root.NumChildren - 1);
        }
        if (FindChildRecursive(guideLayer, "window") is not GObject window)
        {
            return;
        }

        window.SetSize(Math.Max(1f, bagBtn.Width), Math.Max(1f, bagBtn.Height), true);
        var bagAbs = GetAbsolutePosition(bagBtn);
        var layerAbs = GetAbsolutePosition(guideLayer);
        var target = new PointF(bagAbs.X - layerAbs.X, bagAbs.Y - layerAbs.Y);
        var start = new PointF(window.X, window.Y);
        GTween.To(start, target, 0.5f)
            .SetEase(EaseType.QuadOut)
            .SetTarget(window)
            .OnUpdate(t => window.SetXY(t.Value.X, t.Value.Y));
    }

    private static PointF GetAbsolutePosition(GObject obj)
    {
        var x = obj.X;
        var y = obj.Y;
        var p = obj.Parent;
        while (p != null)
        {
            x += p.X;
            y += p.Y;
            p = p.Parent;
        }

        return new PointF(x, y);
    }
}

#endif



