/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TreeView
{
    public partial class TreeItem : GButton
    {
        public Controller expanded;
        public Controller leaf;
        public GGraph indent;
        public GButton expandButton;
        public const string URL = "ui://5nx1f8vzpmk31";

        public static TreeItem CreateInstance()
        {
            return (TreeItem)UIPackage.CreateObject("TreeView", "TreeItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            expanded = GetControllerAt(1);
            leaf = GetControllerAt(2);
            indent = (GGraph)GetChildAt(2);
            expandButton = (GButton)GetChildAt(4);
        }
    }
}