using System.Text.Json;
using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public sealed class FileBackedPlayerProgressRepository : IPlayerProgressRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, PlayerProgress> _storage = new(StringComparer.Ordinal);
    private readonly string _storagePath;
    private readonly int _initialGold;
    private readonly int _initialUnlockedStageId;
    private readonly int _saveRetryCount;
    private bool _loaded;

    public FileBackedPlayerProgressRepository(
        string storagePath,
        int initialGold = 120,
        int initialUnlockedStageId = 1,
        int saveRetryCount = 2)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path cannot be empty.", nameof(storagePath));
        }

        _storagePath = storagePath;
        _initialGold = Math.Max(0, initialGold);
        _initialUnlockedStageId = Math.Max(1, initialUnlockedStageId);
        _saveRetryCount = Math.Max(0, saveRetryCount);
    }

    public PlayerProgress GetOrCreate(string playerId)
    {
        ValidatePlayerId(playerId);

        lock (_gate)
        {
            EnsureLoaded_NoLock();
            if (!_storage.TryGetValue(playerId, out var progress))
            {
                progress = new PlayerProgress
                {
                    PlayerId = playerId,
                    Gold = _initialGold,
                    HighestUnlockedStageId = _initialUnlockedStageId,
                };
                _storage[playerId] = progress.Clone();
            }

            return progress.Clone();
        }
    }

    public void Save(PlayerProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ValidatePlayerId(progress.PlayerId);

        lock (_gate)
        {
            EnsureLoaded_NoLock();
            _storage[progress.PlayerId] = progress.Clone();
            TryPersist_NoLock();
        }
    }

    private void EnsureLoaded_NoLock()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (!File.Exists(_storagePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_storagePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var records = JsonSerializer.Deserialize<Dictionary<string, PersistedPlayerProgress>>(json);
            if (records is null)
            {
                return;
            }

            _storage.Clear();
            foreach (var pair in records)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
                {
                    continue;
                }

                _storage[pair.Key] = pair.Value.ToRuntime();
            }
        }
        catch
        {
            // Keep in-memory fallback when loading fails.
        }
    }

    private void TryPersist_NoLock()
    {
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var payload = _storage.ToDictionary(
            static pair => pair.Key,
            static pair => PersistedPlayerProgress.FromRuntime(pair.Value),
            StringComparer.Ordinal);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false,
        });

        for (var attempt = 0; attempt <= _saveRetryCount; attempt++)
        {
            try
            {
                File.WriteAllText(_storagePath, json);
                return;
            }
            catch
            {
                if (attempt == _saveRetryCount)
                {
                    return;
                }
            }
        }
    }

    private static void ValidatePlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            throw new ArgumentException("Player id cannot be empty.", nameof(playerId));
        }
    }

    private sealed class PersistedPlayerProgress
    {
        public string PlayerId { get; set; } = string.Empty;

        public int Gold { get; set; }

        public int HighestUnlockedStageId { get; set; } = 1;

        public Dictionary<int, int> HeroLevels { get; set; } = new();

        public Dictionary<BattleUnitRole, int> TroopLevels { get; set; } = new();

        public static PersistedPlayerProgress FromRuntime(PlayerProgress runtime)
        {
            return new PersistedPlayerProgress
            {
                PlayerId = runtime.PlayerId,
                Gold = runtime.Gold,
                HighestUnlockedStageId = runtime.HighestUnlockedStageId,
                HeroLevels = new Dictionary<int, int>(runtime.HeroLevels),
                TroopLevels = new Dictionary<BattleUnitRole, int>(runtime.TroopLevels),
            };
        }

        public PlayerProgress ToRuntime()
        {
            var runtime = new PlayerProgress
            {
                PlayerId = PlayerId,
                Gold = Gold,
                HighestUnlockedStageId = Math.Max(1, HighestUnlockedStageId),
            };

            foreach (var pair in HeroLevels)
            {
                runtime.SetHeroLevel(pair.Key, pair.Value);
            }

            foreach (var pair in TroopLevels)
            {
                runtime.SetTroopLevel(pair.Key, pair.Value);
            }

            return runtime;
        }
    }
}
