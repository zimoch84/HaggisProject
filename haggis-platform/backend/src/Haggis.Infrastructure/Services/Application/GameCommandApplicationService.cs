using System.Text.Json;
using System.Text.Json.Nodes;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.GameRooms;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Application;

public sealed class GameCommandApplicationService : IGameCommandApplicationService
{
    private readonly IGameSessionStore _sessionStore;
    private readonly IGameRoomStore _roomStore;

    public GameCommandApplicationService(IGameSessionStore sessionStore, IGameRoomStore roomStore)
    {
        _sessionStore = sessionStore;
        _roomStore = roomStore;
    }

    public GameEventMessage Handle(string gameId, GameClientMessage message)
    {
        var effectiveMessage = EnrichInitializeWithRoomPlayers(gameId, message);
        var session = _sessionStore.GetOrCreate(gameId);
        try
        {
            var applyResult = session.Apply(effectiveMessage);
            return new GameEventMessage(
                Type: "CommandApplied",
                OrderPointer: applyResult.OrderPointer,
                GameId: gameId,
                Error: null,
                Command: effectiveMessage.Command,
                State: applyResult.State,
                CreatedAt: DateTimeOffset.UtcNow,
                CurrentPlayerId: TryExtractCurrentPlayerId(applyResult.State));
        }
        catch (InvalidOperationException ex)
        {
            return new GameEventMessage(
                Type: "CommandRejected",
                OrderPointer: null,
                GameId: gameId,
                Error: ex.Message,
                Command: effectiveMessage.Command,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow);
        }
    }

    private GameClientMessage EnrichInitializeWithRoomPlayers(string gameId, GameClientMessage message)
    {
        if (!message.Command.Type.Equals("Initialize", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        if (!_roomStore.TryGetRoom(gameId, out var room) || room is null)
        {
            return message;
        }

        if (message.Command.Payload.ValueKind == JsonValueKind.Object &&
            message.Command.Payload.TryGetProperty("players", out var playersElement) &&
            playersElement.ValueKind == JsonValueKind.Array)
        {
            return message;
        }

        JsonObject payload;
        if (message.Command.Payload.ValueKind == JsonValueKind.Object)
        {
            payload = JsonNode.Parse(message.Command.Payload.GetRawText()) as JsonObject ?? new JsonObject();
        }
        else
        {
            payload = new JsonObject();
        }

        payload["players"] = JsonSerializer.SerializeToNode(room.Players);
        var enrichedPayload = JsonSerializer.SerializeToElement(payload);
        var enrichedCommand = message.Command with { Payload = enrichedPayload };
        return message with { Command = enrichedCommand };
    }

    private static string? TryExtractCurrentPlayerId(GameStateSnapshot state)
    {
        if (state.Data.ValueKind != JsonValueKind.Object ||
            !state.Data.TryGetProperty("currentPlayerId", out var currentPlayerElement) ||
            currentPlayerElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var currentPlayerId = currentPlayerElement.GetString();
        return string.IsNullOrWhiteSpace(currentPlayerId) ? null : currentPlayerId.Trim();
    }
}
