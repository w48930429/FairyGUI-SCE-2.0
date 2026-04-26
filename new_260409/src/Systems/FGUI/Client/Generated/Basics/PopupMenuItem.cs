/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Basics
{
    public partial class PopupMenuItem : GButton
    {
        public Controller checkedController;
        public const string URL = "ui://9leh0eyfl6f46z";

        public static PopupMenuItem CreateInstance()
        {
            return (PopupMenuItem)UIPackage.CreateObject("Basics", "PopupMenuItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            checkedController = GetControllerAt(1);
        }
    }
}