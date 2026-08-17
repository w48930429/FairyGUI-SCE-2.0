/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Joystick
{
    public partial class circle : GButton
    {
        public GImage thumb;
        public const string URL = "ui://rbw1tvvvq9do18";

        public static circle CreateInstance()
        {
            return (circle)UIPackage.CreateObject("Joystick", "circle");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            thumb = (GImage)GetChildAt(0);
        }
    }
}