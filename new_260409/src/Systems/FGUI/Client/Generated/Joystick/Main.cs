/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Joystick
{
    public partial class Main : GComponent
    {
        public GImage joystick_center;
        public circle joystick;
        public GGraph joystick_touch;
        public const string URL = "ui://rbw1tvvviitt1";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("Joystick", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            joystick_center = (GImage)GetChildAt(0);
            joystick = (circle)GetChildAt(1);
            joystick_touch = (GGraph)GetChildAt(2);
        }
    }
}