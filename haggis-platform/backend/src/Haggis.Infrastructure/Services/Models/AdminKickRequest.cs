namespace Haggis.Infrastructure.Services.Models;

public sealed record AdminKickRequest(string PlayerId, string? Reason = null);
