using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public sealed class InMemoryPlayerProgressRepository : IPlayerProgressRepository
{
    private readonly Dictionary<string, PlayerProgress> _storage = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly int _initialGold;
    private readonly int _initialUnlockedStageId;

    public InMemoryPlayerProgressRepository(int initialGold = 120, int initialUnlockedStageId = 1)
    {
        _initialGold = Math.Max(0, initialGold);
        _initialUnlockedStageId = Math.Max(1, initialUnlockedStageId);
    }

    public PlayerProgress GetOrCreate(string playerId)
    {
        ValidatePlayerId(playerId);

        lock (_gate)
        {
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
            _storage[progress.PlayerId] = progress.Clone();
        }
    }

    private static void ValidatePlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            throw new ArgumentException("Player id cannot be empty.", nameof(playerId));
        }
    }
}
