/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TurnPage
{
    public partial class Main : GComponent
    {
        public Controller bookPos;
        public Book book;
        public GSlider pageSlide;
        public GButton btnPrev;
        public GButton btnNext;
        public const string URL = "ui://ynixt3ubgawe0";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("TurnPage", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bookPos = GetControllerAt(0);
            book = (Book)GetChildAt(0);
            pageSlide = (GSlider)GetChildAt(1);
            btnPrev = (GButton)GetChildAt(2);
            btnNext = (GButton)GetChildAt(3);
        }
    }
}