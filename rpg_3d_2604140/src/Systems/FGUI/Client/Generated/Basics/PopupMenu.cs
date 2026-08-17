/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Basics
{
    public partial class PopupMenu : GComponent
    {
        public GList list;
        public const string URL = "ui://9leh0eyfl6f46x";

        public static PopupMenu CreateInstance()
        {
            return (PopupMenu)UIPackage.CreateObject("Basics", "PopupMenu");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            list = (GList)GetChildAt(1);
        }
    }
}