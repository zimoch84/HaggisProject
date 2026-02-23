namespace Haggis.Infrastructure.Services.Models;

public sealed record GameClientMessage(string Type, GameCommand Command, GameStateSnapshot? State);
