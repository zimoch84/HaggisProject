using System.Text.Json;

namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketOperationParser
{
    public bool TryParse(string text, out GameWebSocketOperationDto? operation)
    {
        operation = null;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (!root.TryGetProperty("operation", out var operationElement))
            {
                return false;
            }

            var name = operationElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var normalizedOperation = name.Trim();
            var operationType = GameWebSocketOperationTypeExtensions.TryParse(normalizedOperation, out var parsedOperationType)
                ? parsedOperationType
                : GameWebSocketOperationType.Unknown;

            var payload = root.TryGetProperty("payload", out var payloadElement)
                ? payloadElement.Clone()
                : default;

            operation = new GameWebSocketOperationDto
            {
                RawOperation = normalizedOperation,
                Operation = operationType,
                Payload = payload
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
