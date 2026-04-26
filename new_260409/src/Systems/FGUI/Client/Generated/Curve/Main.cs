/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Curve
{
    public partial class Main : GComponent
    {
        public GList list;
        public const string URL = "ui://i1i2ucwo113wo0";

        public static Main CreateInstance()
        {
            return (Main)UIPackage.CreateObject("Curve", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            list = (GList)GetChildAt(1);
        }
    }
}