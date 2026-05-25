namespace Haggis.Infrastructure.Services.Application;

public sealed record GameWebSocketAuditEntry(
    DateTimeOffset TimestampUtc,
    string Direction,
    string Delivery,
    string GameId,
    string Operation,
    string? PlayerId,
    int RecipientCount,
    string Payload);
