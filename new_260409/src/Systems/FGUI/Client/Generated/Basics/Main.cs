/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Basics
{
    public partial class Main : GComponent
    {
        public Controller c1;
        public GComponent container;
        public GButton btn_Back;
        public GButton btn_Button;
        public GButton btn_Image;
        public GButton btn_Graph;
        public GButton btn_MovieClip;
        public GButton btn_Depth;
        public GButton btn_Loader;
        public GButton btn_List;
        public GButton btn_ProgressBar;
        public GButton btn_Slider;
        public GButton btn_ComboBox;
        public GButton btn_ClipScroll;
        public GButton btn_Controller;
        public GButton btn_Relation;
        public GButton btn_Label;
        public GButton btn_Popup;
        public GButton btn_Window;
        public GButton btn_DragDrop;
        public GButton btn_Component;
        public GButton btn_Grid;
        public GButton btn_Text;
        public GGroup btns;
        public const string URL = "ui://9leh0eyfrpmb1c";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("Basics", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            c1 = GetControllerAt(0);
            container = (GComponent)GetChildAt(1);
            btn_Back = (GButton)GetChildAt(3);
            btn_Button = (GButton)GetChildAt(4);
            btn_Image = (GButton)GetChildAt(5);
            btn_Graph = (GButton)GetChildAt(6);
            btn_MovieClip = (GButton)GetChildAt(7);
            btn_Depth = (GButton)GetChildAt(8);
            btn_Loader = (GButton)GetChildAt(9);
            btn_List = (GButton)GetChildAt(10);
            btn_ProgressBar = (GButton)GetChildAt(11);
            btn_Slider = (GButton)GetChildAt(12);
            btn_ComboBox = (GButton)GetChildAt(13);
            btn_ClipScroll = (GButton)GetChildAt(14);
            btn_Controller = (GButton)GetChildAt(15);
            btn_Relation = (GButton)GetChildAt(16);
            btn_Label = (GButton)GetChildAt(17);
            btn_Popup = (GButton)GetChildAt(18);
            btn_Window = (GButton)GetChildAt(19);
            btn_DragDrop = (GButton)GetChildAt(20);
            btn_Component = (GButton)GetChildAt(21);
            btn_Grid = (GButton)GetChildAt(22);
            btn_Text = (GButton)GetChildAt(23);
            btns = (GGroup)GetChildAt(24);
        }
    }
}