/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Bag
{
    public partial class CloseButton : GButton
    {
        public Controller c1;
        public const string URL = "ui://rbw1tv9tdwwc4";

        public static CloseButton CreateInstance()
        {
            return (CloseButton)UIPackage.CreateObject("Bag", "CloseButton");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            c1 = GetControllerAt(1);
        }
    }
}