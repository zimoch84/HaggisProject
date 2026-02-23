namespace Haggis.Infrastructure.Services.Models;

public sealed record GameEventMessage(
    string Type,
    long? OrderPointer,
    string GameId,
    string? Error,
    GameCommand? Command,
    GameStateSnapshot? State,
    DateTimeOffset CreatedAt);
