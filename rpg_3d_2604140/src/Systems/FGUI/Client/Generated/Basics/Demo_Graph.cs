/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Basics
{
    public partial class Demo_Graph : GComponent
    {
        public GGraph polygon;
        public GGraph polygon2;
        public GGraph line;
        public GImage line3;
        public GGraph pie;
        public GGraph radial;
        public GGraph trapezoid;
        public GGraph line2;
        public const string URL = "ui://9leh0eyfhixt1m";

        public static Demo_Graph CreateInstance()
        {
            return (Demo_Graph)UIPackage.CreateObject("Basics", "Demo_Graph");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            polygon = (GGraph)GetChildAt(5);
            polygon2 = (GGraph)GetChildAt(6);
            line = (GGraph)GetChildAt(7);
            line3 = (GImage)GetChildAt(8);
            pie = (GGraph)GetChildAt(12);
            radial = (GGraph)GetChildAt(14);
            trapezoid = (GGraph)GetChildAt(15);
            line2 = (GGraph)GetChildAt(16);
        }
    }
}