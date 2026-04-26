#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class TurnCardUILogic : FguiUILogicBase
{
    public override string PackageName => "TurnCard";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        var c0 = FindButtonByName(view, "c0");
        var c1 = FindButtonByName(view, "c1");
        if (c0 == null || c1 == null)
        {
            message = "bind failed: c0/c1 missing";
            return false;
        }

        c0.OnClick.Add(_ => ToggleCard(c0));
        c1.OnClick.Add(_ => ToggleCard(c1));
        message = "turn card bound";
        return true;
    }

    private static void ToggleCard(GButton button)
    {
        var back = button.GetChild("n0");
        var front = button.GetChild("icon");
        if (back == null || front == null)
        {
            return;
        }

        var toOpen = !front.Visible;
        GTween.Kill(button);
        GTween.To(0f, 1f, 0.28f)
            .SetTarget(button)
            .SetEase(EaseType.QuadOut)
            .OnUpdate(t =>
            {
                var p = Math.Clamp(t.Value.X, 0f, 1f);
                var compress = p < 0.5f
                    ? 1f - p * 2f
                    : (p - 0.5f) * 2f;
                var safeCompress = MathF.Max(0.06f, compress);
                button.SetScale(safeCompress, 1f);

                if (p >= 0.5f)
                {
                    front.Visible = toOpen;
                    back.Visible = !toOpen;
                }
            })
            .OnComplete(_ =>
            {
                button.SetScale(1f, 1f);
                front.Visible = toOpen;
                back.Visible = !toOpen;
            });
    }
}

#endif



