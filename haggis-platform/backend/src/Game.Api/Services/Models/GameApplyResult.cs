namespace Game.API.Services.Models;

public sealed record GameApplyResult(long OrderPointer, GameStateSnapshot State);
