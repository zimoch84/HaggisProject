using System.Text.Json.Serialization;

namespace Haggis.Infrastructure.Dtos.Chat;

public sealed record ChatChannelSnapshot(
    [property: JsonPropertyName("channelId")] string ChannelId,
    [property: JsonPropertyName("channelType")] string ChannelType,
    [property: JsonPropertyName("roomId")] string? RoomId = null,
    [property: JsonPropertyName("roomName")] string? RoomName = null,
    [property: JsonPropertyName("gameType")] string? GameType = null);
