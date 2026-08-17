/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Cooldown
{
    public partial class Button2 : GButton
    {
        public GImage btn;
        public GImage mask;
        public const string URL = "ui://y768eypfp3yap";

        public static Button2 CreateInstance()
        {
            return (Button2)UIPackage.CreateObject("Cooldown", "Button2");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            btn = (GImage)GetChildAt(3);
            mask = (GImage)GetChildAt(5);
        }
    }
}