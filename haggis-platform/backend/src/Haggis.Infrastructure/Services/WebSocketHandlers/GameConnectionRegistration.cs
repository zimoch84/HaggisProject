namespace Haggis.Infrastructure.Services.WebSocketHandlers;

public sealed record GameConnectionRegistration(Guid ClientId, Guid ConnectionId);
