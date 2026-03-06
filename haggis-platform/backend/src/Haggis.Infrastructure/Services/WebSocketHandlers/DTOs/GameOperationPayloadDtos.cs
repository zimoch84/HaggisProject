using System.Text.Json;
using System.Text.Json.Serialization;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.WebSocketHandlers;

internal sealed class GameWebSocketJoinPayloadDto
{
    [JsonRequired]
    public string PlayerId { get; init; } = string.Empty;
}

internal sealed class GameWebSocketCreatePayloadDto
{
    [JsonRequired]
    public string PlayerId { get; init; } = string.Empty;
    public GameWebSocketCreateOptionsDto? Payload { get; init; }
}

internal sealed class GameWebSocketCreateOptionsDto
{
    public int? Seed { get; init; }
    public int? PlayerCount { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }

    public GameCommandPayload ToGameCommandPayload()
    {
        return new GameCommandPayload
        {
            Seed = Seed,
            PlayerCount = PlayerCount,
            AdditionalData = AdditionalData
        };
    }
}

internal sealed class GameWebSocketSnapshotPayloadDto
{
    [JsonRequired]
    public string PlayerId { get; init; } = string.Empty;
}

internal sealed class GameWebSocketChatPayloadDto
{
    [JsonRequired]
    public string PlayerId { get; init; } = string.Empty;
    [JsonRequired]
    public string Text { get; init; } = string.Empty;
}

internal sealed class GameWebSocketCommandEnvelopePayloadDto
{
    [JsonRequired]
    public GameWebSocketCommandDto? Command { get; init; }
    public GameStateSnapshot? State { get; init; }
}

internal sealed class GameWebSocketCommandDto
{
    [JsonRequired]
    public string Type { get; init; } = string.Empty;
    [JsonRequired]
    public string PlayerId { get; init; } = string.Empty;
    public GameWebSocketCommandPayloadDto? Payload { get; init; }
}

internal sealed class GameWebSocketCommandPayloadDto
{
    public string? Action { get; init; }
    public string? Trick { get; init; }
    public bool? Pass { get; init; }
    public int? Seed { get; init; }
    public int? PlayerCount { get; init; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }

    public GameCommandPayload ToGameCommandPayload()
    {
        return new GameCommandPayload
        {
            Action = Action,
            Trick = Trick,
            Pass = Pass,
            Seed = Seed,
            PlayerCount = PlayerCount,
            AdditionalData = AdditionalData
        };
    }
}
