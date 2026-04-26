#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal sealed partial class FilterUILogic : FguiUILogicBase
{
    private readonly List<GSlider> sliders = [];

    public override string PackageName => "Filter";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        sliders.Clear();

        for (var i = 0; i <= 4; i++)
        {
            if (FindChildRecursive(view, $"s{i}") is GSlider slider)
            {
                sliders.Add(slider);
            }
        }

        if (sliders.Count == 0)
        {
            message = "bind failed: no sliders found";
            return false;
        }

        var defaults = new[] { 100f, 100f, 100f, 200f, 20f };
        for (var i = 0; i < sliders.Count && i < defaults.Length; i++)
        {
            sliders[i].Value = defaults[i];
            sliders[i].AddEventListener("onChanged", OnSliderChanged);
        }

        ApplySimpleColorShift();
        message = "filter fallback logic active (color shift)";
        return true;
    }

    public override void Cleanup()
    {
        foreach (var slider in sliders)
        {
            slider.RemoveEventListener("onChanged", OnSliderChanged);
        }

        sliders.Clear();
        Root = null;
    }

    private void OnSliderChanged(EventContext _)
    {
        ApplySimpleColorShift();
    }

    private void ApplySimpleColorShift()
    {
        if (Root == null || sliders.Count == 0)
        {
            return;
        }

        var brightness = (sliders[0].Value - 100f) / 100f;
        var c = ClampByte(255f * (1f + brightness * 0.4f));
        foreach (var img in EnumerateChildrenRecursive(Root).OfType<GImage>())
        {
            img.Color = Color.FromArgb(255, c, c, c);
        }
    }

    private static int ClampByte(float value)
    {
        return Math.Clamp((int)MathF.Round(value), 0, 255);
    }
}

#endif



