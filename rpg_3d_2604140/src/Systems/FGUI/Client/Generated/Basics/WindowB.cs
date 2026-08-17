/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Basics
{
    public partial class WindowB : GComponent
    {
        public WindowFrameB frame;
        public Transition t1;
        public const string URL = "ui://9leh0eyf796x7a";

        public static WindowB CreateInstance()
        {
            return (WindowB)UIPackage.CreateObject("Basics", "WindowB");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            frame = (WindowFrameB)GetChildAt(0);
            t1 = GetTransitionAt(0);
        }
    }
}