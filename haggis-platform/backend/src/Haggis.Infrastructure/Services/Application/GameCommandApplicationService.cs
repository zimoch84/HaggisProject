using System.Text.Json;
using System.Text.Json.Nodes;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Application;

public sealed class GameCommandApplicationService : IGameCommandApplicationService
{
    private readonly IGameSessionStore _sessionStore;
    private readonly IGameRoomStore _roomStore;
    private readonly IGameCommandAuditLogger _auditLogger;

    public GameCommandApplicationService(
        IGameSessionStore sessionStore,
        IGameRoomStore roomStore,
        IGameCommandAuditLogger? auditLogger = null)
    {
        _sessionStore = sessionStore;
        _roomStore = roomStore;
        _auditLogger = auditLogger ?? NullGameCommandAuditLogger.Instance;
    }

    public GameEventMessage Handle(string gameId, GameClientMessage message)
    {
        var effectiveMessage = EnrichInitializeWithRoomPlayers(gameId, message);
        LogAcceptedCommand(gameId, effectiveMessage);
        var session = _sessionStore.GetOrCreate(gameId);
        try
        {
            var applyResult = session.Apply(effectiveMessage);
            var response = new GameEventMessage(
                Type: "CommandApplied",
                OrderPointer: applyResult.OrderPointer,
                GameId: gameId,
                Error: null,
                Command: effectiveMessage.Command,
                State: applyResult.State,
                CreatedAt: DateTimeOffset.UtcNow,
                CurrentPlayerId: TryExtractCurrentPlayerId(applyResult.State),
                MessageKind: "response");
            LogCommandResult(effectiveMessage, response);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var response = new GameEventMessage(
                Type: "CommandRejected",
                OrderPointer: null,
                GameId: gameId,
                Error: ex.Message,
                Command: effectiveMessage.Command,
                State: null,
                CreatedAt: DateTimeOffset.UtcNow,
                MessageKind: "response");
            LogCommandResult(effectiveMessage, response);
            return response;
        }
    }

    public GameEventMessage GetSnapshot(string gameId)
    {
        var session = _sessionStore.GetOrCreate(gameId);
        return new GameEventMessage(
            Type: "GameSnapshot",
            OrderPointer: session.OrderPointer,
            GameId: gameId,
            Error: null,
            Command: null,
            State: session.CurrentState,
            CreatedAt: DateTimeOffset.UtcNow,
            CurrentPlayerId: TryExtractCurrentPlayerId(session.CurrentState),
            MessageKind: "response");
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

    private void LogAcceptedCommand(string gameId, GameClientMessage message)
    {
        _auditLogger.Log(new GameCommandAuditEntry(
            TimestampUtc: DateTimeOffset.UtcNow,
            Stage: "accepted",
            GameId: gameId,
            CommandType: message.Command.Type,
            PlayerId: message.Command.PlayerId,
            Payload: message.Command.Payload.GetRawText(),
            ResultType: null,
            OrderPointer: null,
            Error: null));
    }

    private void LogCommandResult(GameClientMessage message, GameEventMessage response)
    {
        _auditLogger.Log(new GameCommandAuditEntry(
            TimestampUtc: DateTimeOffset.UtcNow,
            Stage: "result",
            GameId: response.GameId,
            CommandType: message.Command.Type,
            PlayerId: message.Command.PlayerId,
            Payload: message.Command.Payload.GetRawText(),
            ResultType: response.Type,
            OrderPointer: response.OrderPointer,
            Error: response.Error));
    }

    private sealed class NullGameCommandAuditLogger : IGameCommandAuditLogger
    {
        public static NullGameCommandAuditLogger Instance { get; } = new();

        public void Log(GameCommandAuditEntry entry)
        {
        }
    }
}
