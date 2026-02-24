using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.Chat;

public sealed record ChatMessage(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("playerId")] string PlayerId,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt);

