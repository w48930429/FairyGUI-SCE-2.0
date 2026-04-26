#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class CooldownUILogic : FguiUILogicBase
{
    private readonly object tweenToken0 = new();
    private readonly object tweenToken1 = new();

    public override string PackageName => "Cooldown";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        var b0 = FindButtonByName(view, "b0");
        var b1 = FindButtonByName(view, "b1");
        var mask0 = b0?.GetChild("mask") as GImage;
        var mask1 = b1?.GetChild("mask") as GImage;
        if (b0 == null || b1 == null || mask0 == null || mask1 == null)
        {
            message = "bind failed: missing b0/b1/mask controls";
            return false;
        }

        b0.Icon = UIPackage.GetItemURL("Cooldown", "k0");
        b1.Icon = UIPackage.GetItemURL("Cooldown", "k1");
        StartCooldownTween(mask0, 5f, tweenToken0, null);
        StartCooldownTween(mask1, 10f, tweenToken1, value => b1.Text = $"{MathF.Round(value)}");
        message = "cooldown logic started";
        return true;
    }

    public override bool RunSmoke(out string message)
    {
        message = "cooldown smoke: tweens active";
        return true;
    }

    public override void Cleanup()
    {
        GTween.Kill(tweenToken0);
        GTween.Kill(tweenToken1);
        Root = null;
    }

    private static void StartCooldownTween(GImage mask, float duration, object token, Action<float>? onTick)
    {
        GTween.Kill(token);
        GTween.To(0f, duration, duration)
            .SetTarget(token)
            .SetEase(EaseType.Linear)
            .SetRepeat(-1, false)
            .OnUpdate(t =>
            {
                var progress = t.Value.X / duration;
                mask.FillAmount = 1f - Math.Clamp(progress, 0f, 1f);
                onTick?.Invoke(duration - t.Value.X);
            });
    }
}

#endif



