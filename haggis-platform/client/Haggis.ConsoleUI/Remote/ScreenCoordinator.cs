using System.Text.Json;

public sealed class ScreenCoordinator
{
    private readonly GlobalLobbyWebSocketClient _lobbyClient;
    private readonly string? _defaultPlayerId;

    public ScreenCoordinator(GlobalLobbyWebSocketClient lobbyClient, string? defaultPlayerId = null)
    {
        _lobbyClient = lobbyClient;
        _defaultPlayerId = string.IsNullOrWhiteSpace(defaultPlayerId) ? null : defaultPlayerId.Trim();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var playerId = _defaultPlayerId ?? await new LoginScreen().ShowAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var selectedRoom = await ShowLobbyAsync(playerId.Trim(), cancellationToken);
            if (selectedRoom is null)
            {
                return;
            }

            var gameScreen = new GameScreen();
            var gameLoop = new RemoteGameLoop(
                new RemoteGameOptions(playerId.Trim(), selectedRoom.GameId, _lobbyClient.ServerBaseUrl, null),
                gameScreen);

            var result = await gameLoop.RunAsync(cancellationToken);
            if (result == RemoteGameLoopResult.BackToLobby)
            {
                continue;
            }

            return;
        }
    }

    private async Task<LobbyRoom?> ShowLobbyAsync(string playerId, CancellationToken cancellationToken)
    {
        var screen = new LobbyScreen();
        var state = new LobbyState();

        using var bootstrap = await _lobbyClient.ReceiveAsync(cancellationToken);
        if (bootstrap is not null)
        {
            ApplyLobbyMessage(state, bootstrap.RootElement);
        }

        await _lobbyClient.SendListRoomsAsync(cancellationToken);
        using var roomsResponse = await _lobbyClient.ReceiveAsync(cancellationToken);
        if (roomsResponse is not null)
        {
            ApplyLobbyMessage(state, roomsResponse.RootElement);
        }
        await using var listener = new JsonEventListener(_lobbyClient.ReceiveAsync);

        while (!cancellationToken.IsCancellationRequested)
        {
            var command = await screen.ShowAsync(
                playerId,
                state,
                () => Task.FromResult(DrainLobbyMessages(state, listener)),
                cancellationToken);
            if (string.IsNullOrWhiteSpace(command))
            {
                await _lobbyClient.SendListRoomsAsync(cancellationToken);
                state.Status = "Refreshing rooms...";
                continue;
            }

            if (command.Equals("/quit", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (command.Equals("/refresh", StringComparison.OrdinalIgnoreCase))
            {
                await _lobbyClient.SendListRoomsAsync(cancellationToken);
                state.Status = "Refreshing rooms...";
                continue;
            }

            if (command.StartsWith("/chat ", StringComparison.OrdinalIgnoreCase))
            {
                var text = command[6..].Trim();
                if (text.Length == 0)
                {
                    state.Status = "Chat message cannot be empty.";
                    continue;
                }

                await _lobbyClient.SendChatAsync(playerId, text, cancellationToken);
                state.Status = "Sending chat message...";
                continue;
            }

            if (command.StartsWith("/create", StringComparison.OrdinalIgnoreCase))
            {
                var roomName = command.Length > 7 ? command[7..].Trim() : string.Empty;
                if (roomName.Length == 0)
                {
                    roomName = $"{playerId}'s room";
                }

                var roomId = Slugify(roomName);
                await _lobbyClient.SendCreateRoomAsync(playerId, roomName, roomId, cancellationToken);
                state.Status = $"Creating room '{roomName}'...";
                while (!cancellationToken.IsCancellationRequested)
                {
                    var createdRoom = DrainLobbyMessages(state, listener)
                        ? state.Rooms.FirstOrDefault(room => room.RoomId.Equals(roomId, StringComparison.OrdinalIgnoreCase))
                        : state.Rooms.FirstOrDefault(room => room.RoomId.Equals(roomId, StringComparison.OrdinalIgnoreCase));
                    if (createdRoom is not null)
                    {
                        return createdRoom;
                    }

                    await Task.Delay(50, cancellationToken);
                }

                continue;
            }

            if (command.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
            {
                var token = command[6..].Trim();
                var selected = ResolveRoom(state, token);
                if (selected is null)
                {
                    state.Status = $"Room '{token}' not found.";
                    continue;
                }

                return selected;
            }

            state.Status = "Unknown command.";
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
