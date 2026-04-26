/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Gesture
{
    public partial class Main : GComponent
    {
        public GGraph holder;
        public const string URL = "ui://lua1ugq6t5or0";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("Gesture", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            holder = (GGraph)GetChildAt(0);
        }
    }
}