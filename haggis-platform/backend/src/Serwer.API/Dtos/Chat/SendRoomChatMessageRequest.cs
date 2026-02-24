using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.Chat;

public sealed record SendRoomChatMessageRequest(
    [property: JsonPropertyName("playerId")] string PlayerId,
    [property: JsonPropertyName("text")] string Text);

