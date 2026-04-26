#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class LoopListUILogic : FguiUILogicBase
{
    private GList? list;
    private GComponent? viewRoot;

    public override string PackageName => "LoopList";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        viewRoot = view;
        list = FindListByName(view, "list");
        if (list == null)
        {
            message = "bind failed: list missing";
            return false;
        }

        list.SetVirtualAndLoop();
        list.ItemRenderer = RenderListItem;
        list.NumItems = 5;
        list.AddEventListener("onScroll", OnScroll);
        DoSpecialEffect();
        message = "loop list bound";
        return true;
    }

    public override void Cleanup()
    {
        if (list != null)
        {
            list.RemoveEventListener("onScroll", OnScroll);
        }

        list = null;
        viewRoot = null;
        Root = null;
    }

    private void OnScroll(EventContext _)
    {
        DoSpecialEffect();
    }

    private void DoSpecialEffect()
    {
        if (list == null || viewRoot == null)
        {
            return;
        }

        var scrollPane = list.ScrollPane;
        var midX = (scrollPane?.PosX ?? 0f) + (scrollPane?.ViewWidth ?? list.Width) / 2f;
        for (var i = 0; i < list.NumChildren; i++)
        {
            var obj = list.GetChildAt(i);
            var dist = MathF.Abs(midX - obj.X - obj.Width / 2f);
            if (dist > obj.Width)
            {
                obj.SetScale(1f, 1f);
                continue;
            }

            var scale = 1f + (1f - dist / MathF.Max(1f, obj.Width)) * 0.24f;
            obj.SetScale(scale, scale);
        }

        if (FindTextFieldByName(viewRoot, "n3") is GTextField n3)
        {
            n3.Text = $"{(list.GetFirstChildInView() + 1) % Math.Max(1, list.NumItems)}";
        }
    }

    private static void RenderListItem(int index, GObject obj)
    {
        if (obj is not GButton item)
        {
            return;
        }

        item.SetPivot(0.5f, 0.5f, true);
        item.Icon = UIPackage.GetItemURL("LoopList", $"n{index + 1}");
    }
}

#endif



