#if CLIENT
using FairyGUI;

namespace GameEntry;

internal sealed partial class TypingEffectUILogic : FguiUILogicBase
{
    private readonly object typingToken1 = new();
    private readonly object typingToken2 = new();

    private GTextField? text1;
    private GTextField? text2;
    private string rawText1 = string.Empty;
    private string rawText2 = string.Empty;

    public override string PackageName => "TypingEffect";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        text1 = FindTextFieldByName(view, "n2");
        text2 = FindTextFieldByName(view, "n3");
        if (text1 == null || text2 == null)
        {
            message = "bind failed: missing n2/n3";
            return false;
        }

        rawText1 = text1.Text ?? string.Empty;
        rawText2 = text2.Text ?? string.Empty;
        StartTyping(text1, rawText1, typingToken1, 0.050f);
        StartTyping(text2, rawText2, typingToken2, 0.050f);
        message = "typing effect logic bound";
        return true;
    }

    public override bool RunSmoke(out string message)
    {
        if (text1 == null || text2 == null)
        {
            message = "smoke failed: not bound";
            return false;
        }

        StartTyping(text1, rawText1, typingToken1, 0.050f);
        StartTyping(text2, rawText2, typingToken2, 0.050f);
        message = "typing effect smoke: restarted";
        return true;
    }

    public override void Cleanup()
    {
        GTween.Kill(typingToken1);
        GTween.Kill(typingToken2);
        text1 = null;
        text2 = null;
        Root = null;
    }

    private static void StartTyping(GTextField field, string raw, object token, float charInterval)
    {
        GTween.Kill(token);
        if (string.IsNullOrEmpty(raw))
        {
            field.Text = string.Empty;
            return;
        }

        var total = raw.Length;
        var duration = MathF.Max(0.1f, total * MathF.Max(0.001f, charInterval));
        field.Text = string.Empty;
        GTween.To(0f, total, duration)
            .SetTarget(token)
            .SetEase(EaseType.Linear)
            .OnUpdate(t =>
            {
                var len = Math.Clamp((int)MathF.Ceiling(t.Value.X), 0, total);
                field.Text = len <= 0 ? string.Empty : raw.Substring(0, len);
            })
            .OnComplete(_ => field.Text = raw);
    }
}
#endif
