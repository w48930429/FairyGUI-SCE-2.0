/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TurnPage
{
    public partial class FrontCover : GComponent
    {
        public Controller side;
        public const string URL = "ui://ynixt3ubjva6o";

        public static FrontCover CreateInstance()
        {
            return (FrontCover)UIPackage.CreateObject("TurnPage", "FrontCover");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            side = GetControllerAt(0);
        }
    }
}