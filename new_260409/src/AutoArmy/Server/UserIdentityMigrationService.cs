using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public sealed class UserIdentityMigrationService
{
    private readonly IPlayerProgressRepository _repository;
    private readonly int _defaultInitialGold;
    private readonly object _gate = new();
    private readonly HashSet<string> _migratedTargets = new(StringComparer.Ordinal);

    public UserIdentityMigrationService(IPlayerProgressRepository repository, int defaultInitialGold)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _defaultInitialGold = Math.Max(0, defaultInitialGold);
    }

    public bool EnsureMigrated(string userIdentity, string serverId, int fallbackPlayerSlotId)
    {
        if (string.IsNullOrWhiteSpace(userIdentity))
        {
            throw new ArgumentException("User identity cannot be empty.", nameof(userIdentity));
        }

        if (string.IsNullOrWhiteSpace(serverId))
        {
            throw new ArgumentException("Server id cannot be empty.", nameof(serverId));
        }

        var targetPlayerId = BuildPlayerId(userIdentity, serverId);
        lock (_gate)
        {
            if (_migratedTargets.Contains(targetPlayerId))
            {
                return false;
            }

            var target = _repository.GetOrCreate(targetPlayerId);
            if (!IsDefaultProgress(target))
            {
                _migratedTargets.Add(targetPlayerId);
                return false;
            }

            var candidates = BuildLegacyCandidates(serverId, fallbackPlayerSlotId);
            foreach (var candidatePlayerId in candidates)
            {
                var legacy = _repository.GetOrCreate(candidatePlayerId);
                if (!HasMeaningfulProgress(legacy))
                {
                    continue;
                }

                var migrated = legacy.Clone();
                migrated.PlayerId = targetPlayerId;
                _repository.Save(migrated);
                _migratedTargets.Add(targetPlayerId);
                return true;
            }

            return false;
        }
    }

    private string[] BuildLegacyCandidates(string serverId, int fallbackPlayerSlotId)
    {
        return
        [
            BuildPlayerId($"p:{Math.Max(0, fallbackPlayerSlotId)}", serverId),
            BuildPlayerId("debug-player", serverId),
        ];
    }

    private bool IsDefaultProgress(PlayerProgress progress)
    {
        return progress.Gold == _defaultInitialGold &&
               progress.HighestUnlockedStageId == 1 &&
               progress.HeroLevels.Count == 0 &&
               progress.TroopLevels.Count == 0;
    }

    private bool HasMeaningfulProgress(PlayerProgress progress)
    {
        return progress.Gold != _defaultInitialGold ||
               progress.HighestUnlockedStageId > 1 ||
               progress.HeroLevels.Any(pair => pair.Value > 1) ||
               progress.TroopLevels.Any(pair => pair.Value > 1);
    }

    private static string BuildPlayerId(string identity, string serverId)
    {
        return $"{identity}@{serverId}";
    }
}
