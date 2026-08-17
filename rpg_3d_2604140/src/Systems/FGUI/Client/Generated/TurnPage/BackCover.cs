/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace TurnPage
{
    public partial class BackCover : GComponent
    {
        public Controller side;
        public const string URL = "ui://ynixt3ubjva6p";

        public static BackCover CreateInstance()
        {
            return (BackCover)UIPackage.CreateObject("TurnPage", "BackCover");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            side = GetControllerAt(0);
        }
    }
}