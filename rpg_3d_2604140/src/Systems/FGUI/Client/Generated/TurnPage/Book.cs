/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TurnPage
{
    public partial class Book : GComponent
    {
        public FrontCover frontCover;
        public BackCover backCover;
        public Pages pages;
        public GGraph hotspot_tl;
        public GGraph hotspot_bl;
        public GGraph hotspot_tr;
        public GGraph hotspot_br;
        public const string URL = "ui://ynixt3ubjva6n";

        public static Book CreateInstance()
        {
            return (Book)UIPackage.CreateObject("TurnPage", "Book");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            frontCover = (FrontCover)GetChildAt(0);
            backCover = (BackCover)GetChildAt(1);
            pages = (Pages)GetChildAt(2);
            hotspot_tl = (GGraph)GetChildAt(3);
            hotspot_bl = (GGraph)GetChildAt(4);
            hotspot_tr = (GGraph)GetChildAt(5);
            hotspot_br = (GGraph)GetChildAt(6);
        }
    }
}