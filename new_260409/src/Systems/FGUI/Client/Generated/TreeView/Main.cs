/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TreeView
{
    public partial class Main : GComponent
    {
        public GTree tree;
        public GTree tree2;
        public const string URL = "ui://5nx1f8vzpmk30";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("TreeView", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            tree = (GTree)GetChildAt(1);
            tree2 = (GTree)GetChildAt(3);
        }
    }
}