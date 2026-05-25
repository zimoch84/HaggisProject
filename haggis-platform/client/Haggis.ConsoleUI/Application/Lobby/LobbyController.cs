using System.Text.Json;
using Haggis.ConsoleUI.Presentation.Screens;
using Haggis.ConsoleUI.Presentation.ViewModels.Lobby;

namespace Haggis.ConsoleUI.Application.Lobby;

public sealed class LobbyController
{
    private readonly GlobalLobbyWebSocketClient _lobbyClient;
    private readonly LobbyScreen _screen;

    public LobbyController(GlobalLobbyWebSocketClient lobbyClient, LobbyScreen screen)
    {
        _lobbyClient = lobbyClient;
        _screen = screen;
    }

    public async Task<LobbyRoom?> RunAsync(string playerId, CancellationToken cancellationToken)
    {
        var state = new LobbyState();
        await using var listener = new JsonEventListener(_lobbyClient.ReceiveAsync);

        state.Status = "Loading rooms...";
        await _lobbyClient.SendListRoomsAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var action = await _screen.ShowAsync(
                playerId,
                state,
                () => Task.FromResult(DrainLobbyMessages(state, listener)),
                cancellationToken);

            switch (action)
            {
                case LobbyScreenAction.Refresh:
                    await _lobbyClient.SendListRoomsAsync(cancellationToken);
                    state.Status = "Refreshing rooms...";
                    continue;

                case LobbyScreenAction.Quit:
                    return null;

                case LobbyScreenAction.SendChat sendChat:
                    if (sendChat.Text.Length == 0)
                    {
                        state.Status = "Chat message cannot be empty.";
                        continue;
                    }

                    await _lobbyClient.SendChatAsync(playerId, sendChat.Text, cancellationToken);
                    state.Status = "Sending chat message...";
                    continue;

                case LobbyScreenAction.CreateRoom createRoom:
                {
                    var roomName = string.IsNullOrWhiteSpace(createRoom.RoomName)
                        ? $"{playerId}'s room"
                        : createRoom.RoomName.Trim();
                    var roomId = Slugify(roomName);
                    await _lobbyClient.SendCreateRoomAsync(playerId, roomName, roomId, cancellationToken);
                    state.Status = $"Creating room '{roomName}'...";

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        DrainLobbyMessages(state, listener);
                        var createdRoom = state.Rooms.FirstOrDefault(room =>
                            room.RoomId.Equals(roomId, StringComparison.OrdinalIgnoreCase));
                        if (createdRoom is not null)
                        {
                            return createdRoom;
                        }

                        await Task.Delay(50, cancellationToken);
                    }

                    continue;
                }

                case LobbyScreenAction.JoinRoom joinRoom:
                {
                    var selected = ResolveRoom(state, joinRoom.Token);
                    if (selected is null)
                    {
                        state.Status = $"Room '{joinRoom.Token}' not found.";
                        continue;
                    }

                    return selected;
                }

                case LobbyScreenAction.Unknown:
                    state.Status = "Unknown command.";
                    continue;
            }
        }

        return null;
    }

    private static bool DrainLobbyMessages(LobbyState state, JsonEventListener listener)
    {
        var updated = false;
        while (listener.TryRead(out var message))
        {
            using (message)
            {
                ApplyLobbyMessage(state, message.RootElement);
                updated = true;
            }
        }

        return updated;
    }

    private static LobbyRoom? ApplyLobbyMessage(LobbyState state, JsonElement message)
    {
        var dto = RemoteLobbyMessageParser.Parse(message);
        if (dto is null)
        {
            return null;
        }

        if (string.Equals(dto.Type, "GlobalChatBootstrap", StringComparison.Ordinal))
        {
            state.Messages.Clear();
            state.Messages.AddRange(dto.History?.Select(MapLobbyChatMessage) ?? Enumerable.Empty<LobbyChatMessage>());
            state.Status = "Connected to public lobby.";
            return null;
        }

        if (string.Equals(dto.Type, "ProblemDetails", StringComparison.OrdinalIgnoreCase))
        {
            state.Status = dto.Detail ?? dto.Title ?? dto.Error ?? string.Empty;
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.MessageId))
        {
            state.Messages.Add(new LobbyChatMessage
            {
                PlayerId = dto.PlayerId ?? string.Empty,
                Text = dto.Text ?? string.Empty
            });
            TrimMessages(state);
            state.Status = "Public chat updated.";
            return null;
        }

        if (string.Equals(dto.Operation, "listroom", StringComparison.OrdinalIgnoreCase))
        {
            state.Rooms.Clear();
            state.Rooms.AddRange(dto.Data?.Rooms?.Select(ToLobbyRoom) ?? Enumerable.Empty<LobbyRoom>());
            state.Status = $"Loaded {state.Rooms.Count} rooms.";
            return null;
        }

        if ((string.Equals(dto.Operation, "createroom", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(dto.Operation, "privatechat", StringComparison.OrdinalIgnoreCase)) &&
            dto.Data?.Room is not null)
        {
            var room = ToLobbyRoom(dto.Data.Room);
            ReplaceRoom(state, room);
            state.Status = $"Created room '{room.RoomName}'.";
            return room;
        }

        if (!string.IsNullOrWhiteSpace(dto.Data?.Error))
        {
            state.Status = dto.Data.Error;
        }

        return null;
    }

    private static void ReplaceRoom(LobbyState state, LobbyRoom room)
    {
        var index = state.Rooms.FindIndex(x => x.RoomId.Equals(room.RoomId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            state.Rooms[index] = room;
            return;
        }

        state.Rooms.Add(room);
    }

    private static LobbyRoom? ResolveRoom(LobbyState state, string token)
    {
        if (int.TryParse(token, out var index) && index >= 0 && index < state.Rooms.Count)
        {
            return state.Rooms[index];
        }

        return state.Rooms.FirstOrDefault(room =>
            room.RoomId.Equals(token, StringComparison.OrdinalIgnoreCase) ||
            room.GameId.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    private static LobbyRoom ToLobbyRoom(RemoteLobbyRoomDto element)
    {
        return new LobbyRoom
        {
            RoomId = element.RoomId ?? string.Empty,
            GameId = element.GameId ?? string.Empty,
            RoomName = element.RoomName ?? string.Empty,
            GameEndpoint = element.GameEndpoint ?? string.Empty,
            Players = element.Players?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x ?? string.Empty)
                .ToList() ?? new List<string>()
        };
    }

    private static LobbyChatMessage MapLobbyChatMessage(RemoteLobbyChatMessageDto dto) =>
        new()
        {
            PlayerId = dto.PlayerId ?? string.Empty,
            Text = dto.Text ?? string.Empty
        };

    private static void TrimMessages(LobbyState state)
    {
        const int maxMessages = 12;
        while (state.Messages.Count > maxMessages)
        {
            state.Messages.RemoveAt(0);
        }
    }

    private static string Slugify(string roomName)
    {
        var chars = roomName
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
