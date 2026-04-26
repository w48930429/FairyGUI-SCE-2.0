#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class ExtensionUILogic : FguiUILogicBase
{
    private GList? list;

    public override string PackageName => "Extension";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        list = FindListByName(view, "mailList");
        if (list == null)
        {
            message = "bind failed: mailList missing";
            return false;
        }

        list.RemoveChildrenToPool();
        for (var i = 0; i < 10; i++)
        {
            var item = list.AddItemFromPool() as GButton;
            if (item == null)
            {
                continue;
            }

            if (item.GetChild("timeText") is GTextField timeText)
            {
                timeText.Text = "5 Nov 2015 16:24:33";
            }

            item.Text = $"Mail title {i + 1}";
            if (item.GetController("IsRead") is Controller read)
            {
                read.SelectedIndex = i % 2 == 0 ? 1 : 0;
            }

            if (item.GetController("c1") is Controller fetched)
            {
                fetched.SelectedIndex = i % 3 == 0 ? 1 : 0;
            }
        }

        list.EnsureBoundsCorrect();
        var delay = 0f;
        for (var i = 0; i < list.NumChildren; i++)
        {
            if (list.GetChildAt(i) is not GButton item)
            {
                continue;
            }

            var inView = list.ScrollPane?.IsChildInView(item) ?? true;
            if (!inView)
            {
                break;
            }

            item.GetTransition("t0")?.Play(null, 1, delay);
            delay += 0.2f;
        }

        message = "extension list rendered (+intro effect)";
        return true;
    }

    public override void Cleanup()
    {
        list?.RemoveChildrenToPool();
        list = null;
        Root = null;
    }
}

#endif



