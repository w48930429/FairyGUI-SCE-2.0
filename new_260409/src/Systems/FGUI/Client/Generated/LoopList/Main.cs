/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace LoopList
{
    public partial class Main : GComponent
    {
        public GList list;
        public const string URL = "ui://qf88jb5nrpol0";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("LoopList", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            list = (GList)GetChildAt(0);
        }
    }
}