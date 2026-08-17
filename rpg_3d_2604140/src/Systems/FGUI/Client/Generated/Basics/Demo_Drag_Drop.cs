/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Basics
{
    public partial class Demo_Drag_Drop : GComponent
    {
        public GButton a;
        public GButton b;
        public GButton c;
        public GButton d;
        public const string URL = "ui://9leh0eyfgx2b78";

        public static Demo_Drag_Drop CreateInstance()
        {
            return (Demo_Drag_Drop)UIPackage.CreateObject("Basics", "Demo_Drag&Drop");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            a = (GButton)GetChildAt(0);
            b = (GButton)GetChildAt(1);
            c = (GButton)GetChildAt(2);
            d = (GButton)GetChildAt(7);
        }
    }
}