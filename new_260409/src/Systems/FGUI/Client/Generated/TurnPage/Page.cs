/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TurnPage
{
    public partial class Page : GComponent
    {
        public Controller style;
        public Controller side;
        public GLoader pic;
        public GTextField pn;
        public GGraph model;
        public const string URL = "ui://ynixt3ubgawe1";

        public static Page CreateInstance()
        {
            return (Page)UIPackage.CreateObject("TurnPage", "Page");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            style = GetControllerAt(0);
            side = GetControllerAt(1);
            pic = (GLoader)GetChildAt(1);
            pn = (GTextField)GetChildAt(4);
            model = (GGraph)GetChildAt(5);
        }
    }
}