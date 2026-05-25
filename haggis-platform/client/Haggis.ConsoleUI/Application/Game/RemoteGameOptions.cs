namespace Haggis.ConsoleUI.Application.Game;

public sealed record RemoteGameOptions(
    string PlayerId,
    string GameId,
    string ServerBaseUrl,
    int? Seed);
