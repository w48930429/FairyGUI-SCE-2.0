/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Cooldown
{
    public partial class Button1 : GButton
    {
        public GImage mask;
        public const string URL = "ui://y768eypfltiql";

        public static Button1 CreateInstance()
        {
            return (Button1)UIPackage.CreateObject("Cooldown", "Button1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            mask = (GImage)GetChildAt(3);
        }
    }
}