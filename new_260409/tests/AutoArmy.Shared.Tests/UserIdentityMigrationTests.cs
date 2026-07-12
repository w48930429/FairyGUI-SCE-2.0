using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;
using Xunit;

namespace AutoArmy.Shared.Tests;

public class UserIdentityMigrationTests
{
    [Fact]
    public void EnsureMigrated_WhenLegacyHasProgress_CopiesToUserIdentity()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 180);
        var legacy = repository.GetOrCreate("debug-player@S1");
        legacy.Gold = 999;
        legacy.HighestUnlockedStageId = 3;
        legacy.SetHeroLevel(101, 4);
        repository.Save(legacy);

        var migration = new UserIdentityMigrationService(repository, defaultInitialGold: 180);

        var migrated = migration.EnsureMigrated("u:123456", "S1", fallbackPlayerSlotId: 0);
        var current = repository.GetOrCreate("u:123456@S1");

        Assert.True(migrated);
        Assert.Equal(999, current.Gold);
        Assert.Equal(3, current.HighestUnlockedStageId);
        Assert.Equal(4, current.GetHeroLevel(101));
    }

    [Fact]
    public void EnsureMigrated_WhenTargetAlreadyHasProgress_DoesNotOverwrite()
    {
        var repository = new InMemoryPlayerProgressRepository(initialGold: 180);
        var target = repository.GetOrCreate("u:123456@S1");
        target.Gold = 250;
        target.SetHeroLevel(101, 2);
        repository.Save(target);

        var legacy = repository.GetOrCreate("debug-player@S1");
        legacy.Gold = 900;
        legacy.SetHeroLevel(101, 5);
        repository.Save(legacy);

        var migration = new UserIdentityMigrationService(repository, defaultInitialGold: 180);

        var migrated = migration.EnsureMigrated("u:123456", "S1", fallbackPlayerSlotId: 0);
        var current = repository.GetOrCreate("u:123456@S1");

        Assert.False(migrated);
        Assert.Equal(250, current.Gold);
        Assert.Equal(2, current.GetHeroLevel(101));
    }
}
