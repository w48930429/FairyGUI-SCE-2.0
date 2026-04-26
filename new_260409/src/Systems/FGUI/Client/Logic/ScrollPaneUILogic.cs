#if CLIENT
using FairyGUI;

namespace GameEntry;

internal sealed partial class ScrollPaneUILogic : FguiUILogicBase
{
    private GList? list;
    private GObject? dropBox;
    private GTextField? output;

    public override string PackageName => "ScrollPane";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        list = FindListByName(view, "list");
        dropBox = FindChildRecursive(view, "box");
        output = FindTextFieldByName(view, "txt");
        if (list == null || dropBox == null || output == null)
        {
            message = "bind failed: missing list/box/txt";
            return false;
        }

        list.SetVirtual();
        list.ItemRenderer = RenderListItem;
        list.NumItems = 1000;
        list.OnTouchBegin.Add(OnTouchBeginList);

        dropBox.OnDrop.Add(OnDrop);
        output.Text = "ScrollPane bound";
        message = "scroll pane logic bound";
        return true;
    }

    public override bool RunSmoke(out string message)
    {
        if (list == null)
        {
            message = "smoke failed: list missing";
            return false;
        }

        list.ScrollPane?.ScrollBottom();
        message = "scroll pane smoke: scrolled bottom";
        return true;
    }

    public override void Cleanup()
    {
        if (list != null)
        {
            list.OnTouchBegin.Remove(OnTouchBeginList);
        }

        if (dropBox != null)
        {
            dropBox.OnDrop.Remove(OnDrop);
        }

        list = null;
        dropBox = null;
        output = null;
        Root = null;
    }

    private void RenderListItem(int index, GObject obj)
    {
        if (obj is not GButton item)
        {
            return;
        }

        item.Text = $"Item {index}";
        if (item.ScrollPane != null)
        {
            item.ScrollPane.SetPos(0f, item.ScrollPane.PosY, false);
        }

        var b0 = item.GetChild("b0");
        if (b0 != null)
        {
            b0.OnClick.Clear();
            b0.OnClick.Add(OnClickStick);
        }

        var b1 = item.GetChild("b1");
        if (b1 != null)
        {
            b1.OnClick.Clear();
            b1.OnClick.Add(OnClickDelete);
        }
    }

    private void OnTouchBeginList(EventContext _)
    {
        if (list == null)
        {
            return;
        }

        for (var i = 0; i < list.NumChildren; i++)
        {
            if (list.GetChildAt(i) is not GButton item || item.ScrollPane == null)
            {
                continue;
            }

            if (MathF.Abs(item.ScrollPane.PosX) <= 0.01f)
            {
                continue;
            }

            item.ScrollPane.SetPos(0, item.ScrollPane.PosY, true);
            item.ScrollPane.CancelDragging();
            list.ScrollPane?.CancelDragging();
            break;
        }
    }

    private void OnDrop(EventContext context)
    {
        if (output == null)
        {
            return;
        }

        if (context.Data is DropEventData drop && drop.SourceData is string text && !string.IsNullOrEmpty(text))
        {
            output.Text = $"Drop {text}";
            return;
        }

        output.Text = "Drop";
    }

    private void OnClickStick(EventContext context)
    {
        if (output == null || context.Sender is not GObject sender)
        {
            return;
        }

        output.Text = $"Stick {sender.Parent?.Text ?? "item"}";
    }

    private void OnClickDelete(EventContext context)
    {
        if (output == null || context.Sender is not GObject sender)
        {
            return;
        }

        output.Text = $"Delete {sender.Parent?.Text ?? "item"}";
    }
}
#endif
