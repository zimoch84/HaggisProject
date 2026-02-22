namespace Haggis.API.Services.Models;

public sealed record GameClientMessage(string Type, GameCommand Command, GameStateSnapshot? State);
