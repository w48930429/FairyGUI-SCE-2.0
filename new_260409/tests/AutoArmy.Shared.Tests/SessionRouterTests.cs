using GameEntry.AutoArmy.Server;
using Xunit;

namespace AutoArmy.Shared.Tests;

public class SessionRouterTests
{
    [Fact]
    public void BindAndResolveRoute_ReturnsMatchingRecipient()
    {
        ISessionRouter<string> router = new InMemorySessionRouter<string>();

        router.Bind("user-1", "S1", "recipient-A");
        router.Bind("user-2", "S1", "recipient-B");

        var found = router.TryResolve("user-1", "S1", out var route);

        Assert.True(found);
        Assert.NotNull(route);
        Assert.Equal("recipient-A", route!.Recipient);
    }

    [Fact]
    public void ResolveRoute_WithDifferentServer_DoesNotCrossMatch()
    {
        ISessionRouter<string> router = new InMemorySessionRouter<string>();

        router.Bind("user-1", "S1", "recipient-S1");
        router.Bind("user-1", "S2", "recipient-S2");

        Assert.True(router.TryResolve("user-1", "S1", out var s1));
        Assert.True(router.TryResolve("user-1", "S2", out var s2));
        Assert.NotNull(s1);
        Assert.NotNull(s2);
        Assert.NotEqual(s1!.Recipient, s2!.Recipient);
    }
}
