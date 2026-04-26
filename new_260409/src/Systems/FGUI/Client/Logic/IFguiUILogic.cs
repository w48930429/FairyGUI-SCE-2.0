#if CLIENT
using FairyGUI;

namespace GameEntry;

internal interface IFguiUILogic
{
    string PackageName { get; }
    bool TryBind(GComponent view, out string message);
    bool RunSmoke(out string message);
    void Cleanup();
}
#endif
