#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal static partial class FguiDriverUiUtil
{
    public static void CenterOnRoot(GObject? obj)
    {
        if (obj == null)
        {
            return;
        }

        var rootWidth = UIRuntime.RootWidth;
        var rootHeight = UIRuntime.RootHeight;
        obj.SetXY((rootWidth - obj.Width) * 0.5f, (rootHeight - obj.Height) * 0.5f);
    }
}

#endif

