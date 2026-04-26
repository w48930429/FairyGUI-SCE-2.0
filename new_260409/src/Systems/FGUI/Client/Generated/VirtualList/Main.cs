/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace VirtualList
{
    public partial class Main : GComponent
    {
        public GList mailList;
        public const string URL = "ui://qkteqwfpc8s20";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("VirtualList", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            mailList = (GList)GetChildAt(3);
        }
    }
}