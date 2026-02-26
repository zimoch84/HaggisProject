using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Dtos.Chat;

public sealed record GlobalChatBootstrapMessage(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("channels")] IReadOnlyList<ChatChannelSnapshot> Channels,
    [property: JsonPropertyName("history")] IReadOnlyList<ChatMessage> History,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt);
