namespace GameEntry.AutoArmy.Server;

public sealed class SessionRoute<TRecipient>
{
    public required string PlayerKey { get; init; }

    public required string ServerId { get; init; }

    public required TRecipient Recipient { get; init; }

    public DateTime LastSeenUtc { get; set; }
}

public interface ISessionRouter<TRecipient>
{
    SessionRoute<TRecipient> Bind(string playerKey, string serverId, TRecipient recipient);

    bool TryResolve(string playerKey, string serverId, out SessionRoute<TRecipient>? route);

    bool Unbind(string playerKey, string serverId);
}

public sealed class InMemorySessionRouter<TRecipient> : ISessionRouter<TRecipient>
{
    private readonly object _gate = new();
    private readonly Dictionary<string, SessionRoute<TRecipient>> _routes = new(StringComparer.Ordinal);

    public SessionRoute<TRecipient> Bind(string playerKey, string serverId, TRecipient recipient)
    {
        Validate(playerKey, serverId);
        var key = ComposeKey(playerKey, serverId);
        lock (_gate)
        {
            var route = new SessionRoute<TRecipient>
            {
                PlayerKey = playerKey,
                ServerId = serverId,
                Recipient = recipient,
                LastSeenUtc = DateTime.UtcNow,
            };
            _routes[key] = route;
            return route;
        }
    }

    public bool TryResolve(string playerKey, string serverId, out SessionRoute<TRecipient>? route)
    {
        Validate(playerKey, serverId);
        var key = ComposeKey(playerKey, serverId);
        lock (_gate)
        {
            if (_routes.TryGetValue(key, out var found))
            {
                found.LastSeenUtc = DateTime.UtcNow;
                route = found;
                return true;
            }
        }

        route = null;
        return false;
    }

    public bool Unbind(string playerKey, string serverId)
    {
        Validate(playerKey, serverId);
        var key = ComposeKey(playerKey, serverId);
        lock (_gate)
        {
            return _routes.Remove(key);
        }
    }

    private static string ComposeKey(string playerKey, string serverId)
    {
        return $"{playerKey}@{serverId}";
    }

    private static void Validate(string playerKey, string serverId)
    {
        if (string.IsNullOrWhiteSpace(playerKey))
        {
            throw new ArgumentException("Player key cannot be empty.", nameof(playerKey));
        }

        if (string.IsNullOrWhiteSpace(serverId))
        {
            throw new ArgumentException("Server id cannot be empty.", nameof(serverId));
        }
    }
}
