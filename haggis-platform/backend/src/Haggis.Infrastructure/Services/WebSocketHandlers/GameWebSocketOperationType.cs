namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal enum GameWebSocketOperationType
{
    Join,
    Create,
    Chat,
    Command,
    Snapshot,
    Unknown
}
