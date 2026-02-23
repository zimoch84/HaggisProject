namespace Haggis.Infrastructure.Services.Models;

public sealed record GameApplyResult(long OrderPointer, GameStateSnapshot State);
