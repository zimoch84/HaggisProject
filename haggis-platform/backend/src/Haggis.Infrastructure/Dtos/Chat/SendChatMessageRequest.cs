using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Dtos.Chat;

public sealed record SendChatMessageRequest(
    [property: JsonPropertyName("playerId")] string PlayerId,
    [property: JsonPropertyName("text")] string Text);


