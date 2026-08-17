/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace VirtualList
{
    public partial class mailItem : GButton
    {
        public Controller IsRead;
        public Controller c1;
        public GTextField timeText;
        public Transition t0;
        public const string URL = "ui://qkteqwfpc8s24";

        public static mailItem CreateInstance()
        {
            return (mailItem)UIPackage.CreateObject("VirtualList", "mailItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            IsRead = GetControllerAt(0);
            c1 = GetControllerAt(2);
            timeText = (GTextField)GetChildAt(5);
            t0 = GetTransitionAt(0);
        }
    }
}