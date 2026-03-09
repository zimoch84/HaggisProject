namespace Haggis.Infrastructure.Services.Application;

public sealed record GameCommandAuditEntry(
    DateTimeOffset TimestampUtc,
    string Stage,
    string GameId,
    string CommandType,
    string PlayerId,
    string Payload,
    string? ResultType,
    long? OrderPointer,
    string? Error);
