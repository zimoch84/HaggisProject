public sealed record RemoteGameOptions(
    string PlayerId,
    string GameId,
    string ServerBaseUrl,
    bool AutoStart,
    int? Seed);
