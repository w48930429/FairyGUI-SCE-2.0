using FairyGUI;
using Xunit;

namespace FairyGUI.Tests;

public class FGUIRootTests
{
    [Fact]
    public void FullScreenRoot_IsAComponent()
    {
        var root = new FGUIRoot();

        Assert.IsAssignableFrom<GComponent>(root);
    }
}
