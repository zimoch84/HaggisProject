using System.Text.Json;
namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketOperationParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true
    };

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

            operation = operationType switch
            {
                GameWebSocketOperationType.Command => DeserializeOperation<GameWebSocketCommandOperationDto>(text),
                GameWebSocketOperationType.Join => DeserializeOperation<GameWebSocketJoinOperationDto>(text),
                GameWebSocketOperationType.Create => DeserializeOperation<GameWebSocketCreateOperationDto>(text),
                GameWebSocketOperationType.Chat => DeserializeOperation<GameWebSocketChatOperationDto>(text),
                GameWebSocketOperationType.Snapshot => DeserializeOperation<GameWebSocketSnapshotOperationDto>(text),
                _ => new GameWebSocketUnknownOperationDto(normalizedOperation)
            };
            return operation is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static TOperation? DeserializeOperation<TOperation>(string text)
        where TOperation : GameWebSocketOperationDto
    {
        return JsonSerializer.Deserialize<TOperation>(text, SerializerOptions);
    }
}
