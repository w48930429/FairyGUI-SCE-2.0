#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class VirtualListUILogic : FguiUILogicBase
{
    private GList? list;

    public override string PackageName => "VirtualList";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        list = FindListByName(view, "mailList");
        if (list == null)
        {
            message = "bind failed: mailList missing";
            return false;
        }

        if (FindChildRecursive(view, "n6") is GObject n6)
        {
            n6.OnClick.Add(_ => list.AddSelection(500, true));
        }

        if (FindChildRecursive(view, "n7") is GObject n7)
        {
            n7.OnClick.Add(_ => list.ScrollPane?.ScrollTop());
        }

        if (FindChildRecursive(view, "n8") is GObject n8)
        {
            n8.OnClick.Add(_ => list.ScrollPane?.ScrollBottom());
        }

        list.SetVirtual();
        list.ItemRenderer = RenderItem;
        list.NumItems = 1000;
        message = "virtual list bound";
        return true;
    }

    private static void RenderItem(int index, GObject obj)
    {
        if (obj is not GButton button)
        {
            return;
        }

        if (button.GetController("IsRead") is Controller read)
        {
            read.SelectedIndex = index % 2 == 0 ? 1 : 0;
        }

        if (button.GetController("c1") is Controller fetched)
        {
            fetched.SelectedIndex = index % 3 == 0 ? 1 : 0;
        }

        if (button.GetChild("timeText") is GTextField timeText)
        {
            timeText.Text = "5 Nov 2015 16:24:33";
        }

        button.Text = $"{index} Mail title here";
    }
}

#endif



