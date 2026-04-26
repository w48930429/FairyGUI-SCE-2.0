/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TurnPage
{
    public partial class Pages : GComponent
    {
        public Page left;
        public Page right;
        public const string URL = "ui://ynixt3ubjva6f";

        public static Pages CreateInstance()
        {
            return (Pages)UIPackage.CreateObject("TurnPage", "Pages");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            left = (Page)GetChildAt(0);
            right = (Page)GetChildAt(1);
        }
    }
}