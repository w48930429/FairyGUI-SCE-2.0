using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;
using Xunit;

namespace AutoArmy.Shared.Tests;

public class FileBackedPlayerProgressRepositoryTests
{
    [Fact]
    public void SaveThenReload_PreservesPlayerProgress()
    {
        var storagePath = Path.Combine(Path.GetTempPath(), $"autoarmy-progress-{Guid.NewGuid():N}.json");
        try
        {
            var repository = new FileBackedPlayerProgressRepository(storagePath, initialGold: 50);
            var progress = repository.GetOrCreate("u:1001@S1");
            progress.Gold = 321;
            progress.HighestUnlockedStageId = 3;
            progress.SetHeroLevel(101, 5);
            repository.Save(progress);

            var reloadedRepository = new FileBackedPlayerProgressRepository(storagePath, initialGold: 1);
            var reloaded = reloadedRepository.GetOrCreate("u:1001@S1");

            Assert.Equal(321, reloaded.Gold);
            Assert.Equal(3, reloaded.HighestUnlockedStageId);
            Assert.Equal(5, reloaded.GetHeroLevel(101));
        }
        finally
        {
            if (File.Exists(storagePath))
            {
                File.Delete(storagePath);
            }
        }
    }

    [Fact]
    public void Save_WhenStorageWriteFails_KeepsInMemoryData()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"autoarmy-progress-block-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        try
        {
            var repository = new FileBackedPlayerProgressRepository(directoryPath, initialGold: 50);
            var progress = repository.GetOrCreate("u:1002@S1");
            progress.Gold = 777;

            repository.Save(progress);

            var loaded = repository.GetOrCreate("u:1002@S1");
            Assert.Equal(777, loaded.Gold);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
