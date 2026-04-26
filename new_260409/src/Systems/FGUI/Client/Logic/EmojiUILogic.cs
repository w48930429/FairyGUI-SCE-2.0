#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class EmojiUILogic : FguiUILogicBase
{
    private static readonly string[] EmojiTags =
    [
        "88", "am", "bs", "bz", "ch", "cool", "dhq", "dn",
        "fd", "gz", "han", "hx", "hxiao", "hxiu"
    ];

    private readonly List<EmojiMessage> messages = [];
    private GList? list;
    private GTextInput? input1;
    private GTextInput? input2;
    private GComponent? emojiSelect1;
    private GComponent? emojiSelect2;
    private GObject? btnSend1;
    private GObject? btnSend2;
    private GObject? btnEmoji1;
    private GObject? btnEmoji2;

    public override string PackageName => "Emoji";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        list = FindListByName(view, "list");
        input1 = FindTextInputByName(view, "input1");
        input2 = FindTextInputByName(view, "input2");
        if (list == null)
        {
            message = "bind failed: list missing";
            return false;
        }

        list.SetVirtual();
        list.ItemProvider = GetItemResource;
        list.ItemRenderer = RenderListItem;

        btnSend1 = FindChildRecursive(view, "btnSend1");
        btnSend2 = FindChildRecursive(view, "btnSend2");
        btnEmoji1 = FindChildRecursive(view, "btnEmoji1");
        btnEmoji2 = FindChildRecursive(view, "btnEmoji2");
        btnSend1?.OnClick.Add(OnSend1);
        btnSend2?.OnClick.Add(OnSend2);
        btnEmoji1?.OnClick.Add(OnOpenEmoji1);
        btnEmoji2?.OnClick.Add(OnOpenEmoji2);
        input1?.OnSubmit.Add(OnSend1);
        input2?.OnSubmit.Add(OnSend2);

        emojiSelect1 = UIPackage.CreateObject("Emoji", "EmojiSelectUI") as GComponent;
        if (emojiSelect1?.GetChild("list") is GList emojiList1)
        {
            emojiList1.OnClickItem.Add(OnPickEmoji1);
        }

        emojiSelect2 = UIPackage.CreateObject("Emoji", "EmojiSelectUI_ios") as GComponent;
        if (emojiSelect2?.GetChild("list") is GList emojiList2)
        {
            emojiList2.OnClickItem.Add(OnPickEmoji2);
        }

        AddMessage("FairyGUI", "r1", "Today is a good day :cool", false);
        message = "emoji chat bound";
        return true;
    }

    public override void Cleanup()
    {
        btnSend1?.OnClick.Remove(OnSend1);
        btnSend2?.OnClick.Remove(OnSend2);
        btnEmoji1?.OnClick.Remove(OnOpenEmoji1);
        btnEmoji2?.OnClick.Remove(OnOpenEmoji2);
        input1?.OnSubmit.Remove(OnSend1);
        input2?.OnSubmit.Remove(OnSend2);

        if (emojiSelect1?.GetChild("list") is GList emojiList1)
        {
            emojiList1.OnClickItem.Remove(OnPickEmoji1);
        }

        if (emojiSelect2?.GetChild("list") is GList emojiList2)
        {
            emojiList2.OnClickItem.Remove(OnPickEmoji2);
        }

        emojiSelect1?.RemoveFromParent();
        emojiSelect2?.RemoveFromParent();
        emojiSelect1?.Dispose();
        emojiSelect2?.Dispose();

        messages.Clear();
        list = null;
        input1 = null;
        input2 = null;
        emojiSelect1 = null;
        emojiSelect2 = null;
        btnSend1 = null;
        btnSend2 = null;
        btnEmoji1 = null;
        btnEmoji2 = null;
        Root = null;
    }

    private void OnSend1(EventContext _)
    {
        SendFromInput(input1);
    }

    private void OnSend2(EventContext _)
    {
        SendFromInput(input2);
    }

    private void OnOpenEmoji1(EventContext context)
    {
        if (emojiSelect1 != null && context.Sender is GObject sender)
        {
            ShowEmojiPopup(emojiSelect1, sender, PopupDirection.Up);
        }
    }

    private void OnOpenEmoji2(EventContext context)
    {
        if (emojiSelect2 != null && context.Sender is GObject sender)
        {
            ShowEmojiPopup(emojiSelect2, sender, PopupDirection.Up);
        }
    }

    private void OnPickEmoji1(EventContext context)
    {
        if (input1 == null || context.Data is not GButton item)
        {
            return;
        }

        var code = item.Text?.Trim();
        if (!string.IsNullOrEmpty(code))
        {
            input1.Text = $"{input1.Text}[:{code}]";
        }

        if (emojiSelect1 != null)
        {
            HideEmojiPopup(emojiSelect1);
        }
    }

    private void OnPickEmoji2(EventContext context)
    {
        if (input2 == null || context.Data is not GButton item)
        {
            return;
        }

        var icon = item.Icon;
        var appended = false;
        if (!string.IsNullOrEmpty(icon))
        {
            var pi = UIPackage.GetItemByURL(icon);
            if (pi != null && int.TryParse(pi.Name, System.Globalization.NumberStyles.HexNumber, null, out var codepoint))
            {
                input2.Text = $"{input2.Text}{char.ConvertFromUtf32(codepoint)}";
                appended = true;
            }
        }

        if (!appended)
        {
            var fallback = item.Text?.Trim();
            if (!string.IsNullOrEmpty(fallback))
            {
                input2.Text = $"{input2.Text}{fallback}";
            }
        }

        if (emojiSelect2 != null)
        {
            HideEmojiPopup(emojiSelect2);
        }
    }

    private void ShowEmojiPopup(GComponent popup, GObject sender, PopupDirection direction)
    {
        var host = ResolvePopupHost(sender) ?? Root;
        if (host == null)
        {
            return;
        }

        if (popup.Parent == host && popup.Visible)
        {
            popup.Visible = false;
            popup.RemoveFromParent();
            return;
        }

        if (popup.Parent != host)
        {
            popup.RemoveFromParent();
            host.AddChild(popup);
        }
        else
        {
            host.SetChildIndex(popup, host.NumChildren - 1);
        }

        var anchor = ResolvePositionRelativeToHost(sender, host);
        var x = anchor.X;
        var y = anchor.Y;
        if (direction == PopupDirection.Up)
        {
            y = anchor.Y - popup.Height;
            if (y < 0f)
            {
                y = anchor.Y + sender.Height;
            }
        }
        else
        {
            y = anchor.Y + sender.Height;
            if (y + popup.Height > host.Height)
            {
                y = anchor.Y - popup.Height;
            }
        }

        var maxX = Math.Max(0f, host.Width - popup.Width);
        var maxY = Math.Max(0f, host.Height - popup.Height);
        popup.SetXY(Math.Clamp(x, 0f, maxX), Math.Clamp(y, 0f, maxY));
        popup.Visible = true;
    }

    private static void HideEmojiPopup(GComponent popup)
    {
        popup.Visible = false;
        popup.RemoveFromParent();
    }

    private static GComponent? ResolvePopupHost(GObject target)
    {
        GComponent? host = null;
        GObject? cursor = target.Parent;
        while (cursor != null)
        {
            if (cursor is GComponent component)
            {
                host = component;
            }

            cursor = cursor.Parent;
        }

        return host;
    }

    private static PointF ResolvePositionRelativeToHost(GObject target, GComponent host)
    {
        var x = target.X;
        var y = target.Y;
        GObject? cursor = target.Parent;
        while (cursor != null && cursor != host)
        {
            x += cursor.X;
            y += cursor.Y;
            cursor = cursor.Parent;
        }

        return cursor == host ? new PointF(x, y) : new PointF(target.X, target.Y);
    }

    private void SendFromInput(GTextInput? input)
    {
        if (input == null)
        {
            return;
        }

        var text = (input.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        AddMessage("Unity", "r0", text, true);
        input.Text = string.Empty;
    }

    private void AddMessage(string sender, string senderIcon, string msg, bool fromMe)
    {
        var parsed = ParseEmojiMarkup(msg);
        messages.Add(new EmojiMessage(sender, senderIcon, parsed, fromMe));
        if (fromMe && Random.Shared.NextDouble() < 0.5)
        {
            messages.Add(new EmojiMessage("FairyGUI", "r1", ParseEmojiMarkup("Reply: nice! :cool"), false));
        }

        while (messages.Count > 100)
        {
            messages.RemoveAt(0);
        }

        if (list == null)
        {
            return;
        }

        list.NumItems = messages.Count;
        list.ScrollToView(messages.Count - 1, false);
        list.ScrollPane?.ScrollBottom();
    }

    private string? GetItemResource(int index)
    {
        if (index < 0 || index >= messages.Count)
        {
            return null;
        }

        var item = messages[index];
        return UIPackage.GetItemURL("Emoji", item.FromMe ? "chatRight" : "chatLeft");
    }

    private void RenderListItem(int index, GObject obj)
    {
        if (index < 0 || index >= messages.Count || obj is not GButton item)
        {
            return;
        }

        var msg = messages[index];
        if (!msg.FromMe && item.GetChild("name") is GTextField name)
        {
            name.Text = msg.Sender;
        }

        item.Icon = UIPackage.GetItemURL("Emoji", msg.SenderIcon);
        if (item.GetChild("msg") is GRichTextField msgText)
        {
            msgText.Text = msg.Msg;
        }
    }

    private static string ParseEmojiMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var parsed = text;
        foreach (var tag in EmojiTags)
        {
            var url = UIPackage.GetItemURL("Emoji", tag);
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            parsed = parsed.Replace($"[:{tag}]", $"<img src='{url}'/>", StringComparison.OrdinalIgnoreCase);
            parsed = parsed.Replace($":{tag}", $"<img src='{url}'/>", StringComparison.OrdinalIgnoreCase);
        }

        return parsed;
    }

    private readonly record struct EmojiMessage(string Sender, string SenderIcon, string Msg, bool FromMe);
}

#endif



