namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal static class GameWebSocketOperationTypeExtensions
{
    public static bool TryParse(string value, out GameWebSocketOperationType operationType)
    {
        operationType = value.Trim().ToLowerInvariant() switch
        {
            "join" => GameWebSocketOperationType.Join,
            "create" => GameWebSocketOperationType.Create,
            "chat" => GameWebSocketOperationType.Chat,
            "command" => GameWebSocketOperationType.Command,
            "snapshot" => GameWebSocketOperationType.Snapshot,
            "unknown" => GameWebSocketOperationType.Unknown,
            _ => default
        };

        return value.Trim().ToLowerInvariant() is "join" or "create" or "chat" or "command" or "snapshot" or "unknown";
    }

    public static string ToWireValue(this GameWebSocketOperationType operationType)
    {
        return operationType switch
        {
            GameWebSocketOperationType.Join => "join",
            GameWebSocketOperationType.Create => "create",
            GameWebSocketOperationType.Chat => "chat",
            GameWebSocketOperationType.Command => "command",
            GameWebSocketOperationType.Snapshot => "snapshot",
            GameWebSocketOperationType.Unknown => "unknown",
            _ => "unknown"
        };
    }
}
