/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Cooldown
{
    public partial class Main : GComponent
    {
        public Button1 b0;
        public Button2 b1;
        public const string URL = "ui://y768eypffvaib";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("Cooldown", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            b0 = (Button1)GetChildAt(0);
            b1 = (Button2)GetChildAt(1);
        }
    }
}