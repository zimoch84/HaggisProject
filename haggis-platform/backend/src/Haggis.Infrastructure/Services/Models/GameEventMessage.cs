namespace Haggis.Infrastructure.Services.Models;

public sealed record GameEventMessage(
    string Type,  //powinien byc enum
    long? OrderPointer,
    string GameId,
    string? Error,
    GameCommand? Command,
    GameStateSnapshot? State,
    DateTimeOffset CreatedAt,
    GameChatMessage? Chat = null,
    string? CurrentPlayerId = null,
    string MessageKind = "response");
