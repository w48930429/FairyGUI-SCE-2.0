/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Bag
{
    public partial class WindowFrame : GLabel
    {
        public GGraph dragArea;
        public CloseButton closeButton;
        public const string URL = "ui://rbw1tv9tdwwc3";

        public static WindowFrame CreateInstance()
        {
            return (WindowFrame)UIPackage.CreateObject("Bag", "WindowFrame");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            dragArea = (GGraph)GetChildAt(1);
            closeButton = (CloseButton)GetChildAt(3);
        }
    }
}