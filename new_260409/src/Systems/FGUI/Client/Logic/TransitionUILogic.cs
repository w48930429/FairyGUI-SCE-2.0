#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class TransitionUILogic : FguiUILogicBase
{
    private GGroup? buttonGroup;
    private readonly Dictionary<string, GComponent> cache = new(StringComparer.OrdinalIgnoreCase);
    private float startValue;
    private float endValue;

    public override string PackageName => "Transition";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        buttonGroup = FindChildRecursive(view, "g0") as GGroup;

        BindPlayButton(view, "btn0", "BOSS");
        BindPlayButton(view, "btn1", "BOSS_SKILL");
        BindPlayButton(view, "btn2", "TRAP");
        BindPlayButton(view, "btn5", "PathDemo");

        if (FindChildRecursive(view, "btn3") is GObject btn3)
        {
            btn3.OnClick.Add(_ => PlayGoodHit());
        }

        if (FindChildRecursive(view, "btn4") is GObject btn4)
        {
            btn4.OnClick.Add(_ => PlayPowerUp());
        }

        message = "transition logic bound";
        return true;
    }

    public override void Cleanup()
    {
        foreach (var com in cache.Values)
        {
            com.RemoveFromParent();
            if (!com.Disposed)
            {
                com.Dispose();
            }
        }

        cache.Clear();
        buttonGroup = null;
        Root = null;
    }

    private void BindPlayButton(GComponent root, string btnName, string objectName)
    {
        if (FindChildRecursive(root, btnName) is not GObject btn)
        {
            return;
        }

        btn.OnClick.Add(_ =>
        {
            var target = GetOrCreateComponent(objectName);
            if (target != null)
            {
                PlayTransitionComponent(target);
            }
        });
    }

    private GComponent? GetOrCreateComponent(string name)
    {
        if (cache.TryGetValue(name, out var found))
        {
            return found;
        }

        var created = UIPackage.CreateObject("Transition", name) as GComponent;
        if (created == null)
        {
            return null;
        }

        cache[name] = created;
        return created;
    }

    private void PlayTransitionComponent(GComponent target)
    {
        if (buttonGroup == null || Root == null)
        {
            return;
        }

        buttonGroup.Visible = false;
        AttachToHost(Root, target);
        FguiDriverUiUtil.CenterOnRoot(target);
        target.GetTransition("t0")?.Play(() =>
        {
            buttonGroup.Visible = true;
            target.RemoveFromParent();
        });
    }

    private void PlayGoodHit()
    {
        var g4 = GetOrCreateComponent("GoodHit");
        if (g4 == null || buttonGroup == null || Root == null)
        {
            return;
        }

        buttonGroup.Visible = false;
        g4.SetXY(Root.Width - g4.Width - 20f, 100f);
        AttachToHost(Root, g4);
        g4.GetTransition("t0")?.Play(() =>
        {
            buttonGroup.Visible = true;
            g4.RemoveFromParent();
        }, 3, 0);
    }

    private void PlayPowerUp()
    {
        var g5 = GetOrCreateComponent("PowerUp");
        if (g5 == null || buttonGroup == null || Root == null)
        {
            return;
        }

        buttonGroup.Visible = false;
        g5.SetXY(20f, Root.Height - g5.Height - 100f);
        AttachToHost(Root, g5);

        startValue = 10000f;
        var add = Random.Shared.Next(1000, 3001);
        endValue = startValue + add;
        g5.GetChild("value").Text = $"{startValue:0}";
        g5.GetChild("add_value").Text = $"+{add}";
        GTween.To(startValue, endValue, 0.3f)
            .SetEase(EaseType.Linear)
            .SetTarget(g5)
            .OnUpdate(t => g5.GetChild("value").Text = $"{MathF.Floor(t.Value.X):0}");

        g5.GetTransition("t0")?.Play(() =>
        {
            buttonGroup.Visible = true;
            g5.RemoveFromParent();
        });
    }

    private static void AttachToHost(GComponent host, GComponent target)
    {
        if (target.Parent != host)
        {
            target.RemoveFromParent();
            host.AddChild(target);
        }
        else
        {
            host.SetChildIndex(target, host.NumChildren - 1);
        }
    }
}

#endif



