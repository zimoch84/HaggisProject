using System.Text.Json;

public sealed class RemoteAppLoop
{
    private readonly string _serverBaseUrl;
    private readonly string? _defaultPlayerId;
    private readonly StaticConsoleUI _ui;

    public RemoteAppLoop(string serverBaseUrl, string? defaultPlayerId = null)
    {
        _serverBaseUrl = serverBaseUrl;
        _defaultPlayerId = string.IsNullOrWhiteSpace(defaultPlayerId) ? null : defaultPlayerId.Trim();
        _ui = new StaticConsoleUI();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var playerId = _defaultPlayerId ?? new LoginScreen(_ui).Show();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var selectedRoom = await ShowLobbyAsync(playerId.Trim(), cancellationToken);
            if (selectedRoom is null)
            {
                return;
            }

            var gameScreen = new GameScreen(_ui);
            var gameLoop = new RemoteGameLoop(
                new RemoteGameOptions(playerId.Trim(), selectedRoom.GameId, _serverBaseUrl, false, null),
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
        var screen = new LobbyScreen(_ui);
        var state = new LobbyState();

        await using var client = new GlobalLobbyWebSocketClient();
        await client.ConnectAsync(_serverBaseUrl, cancellationToken);

        using var bootstrap = await client.ReceiveAsync(cancellationToken);
        if (bootstrap is not null)
        {
            ApplyLobbyMessage(state, bootstrap.RootElement);
        }

        await client.SendListRoomsAsync(cancellationToken);
        using var roomsResponse = await client.ReceiveAsync(cancellationToken);
        if (roomsResponse is not null)
        {
            ApplyLobbyMessage(state, roomsResponse.RootElement);
        }
        await using var listener = new JsonEventListener(client.ReceiveAsync);

        while (!cancellationToken.IsCancellationRequested)
        {
            var command = await screen.ShowAsync(
                playerId,
                state,
                () => Task.FromResult(DrainLobbyMessages(state, listener)),
                cancellationToken);
            if (string.IsNullOrWhiteSpace(command))
            {
                await client.SendListRoomsAsync(cancellationToken);
                state.Status = "Refreshing rooms...";
                continue;
            }

            if (command.Equals("/quit", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (command.Equals("/refresh", StringComparison.OrdinalIgnoreCase))
            {
                await client.SendListRoomsAsync(cancellationToken);
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

                await client.SendChatAsync(playerId, text, cancellationToken);
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
                await client.SendCreateRoomAsync(playerId, roomName, roomId, cancellationToken);
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
        if (TryReadString(message, "type", out var type))
        {
            if (type.Equals("GlobalChatBootstrap", StringComparison.Ordinal))
            {
                state.Messages.Clear();
                if (message.TryGetProperty("history", out var historyElement) && historyElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var historyItem in historyElement.EnumerateArray())
                    {
                        state.Messages.Add(new LobbyChatMessage
                        {
                            PlayerId = ReadString(historyItem, "playerId"),
                            Text = ReadString(historyItem, "text")
                        });
                    }
                }

                state.Status = "Connected to public lobby.";
                return null;
            }

            if (type.Equals("ProblemDetails", StringComparison.OrdinalIgnoreCase))
            {
                state.Status = ReadString(message, "error");
                return null;
            }
        }

        if (TryReadString(message, "messageId", out _))
        {
            state.Messages.Add(new LobbyChatMessage
            {
                PlayerId = ReadString(message, "playerId"),
                Text = ReadString(message, "text")
            });
            TrimMessages(state);
            state.Status = "Public chat updated.";
            return null;
        }

        if (TryReadString(message, "operation", out var operation))
        {
            if (operation.Equals("listroom", StringComparison.OrdinalIgnoreCase) &&
                message.TryGetProperty("data", out var listData) &&
                listData.TryGetProperty("rooms", out var roomsElement) &&
                roomsElement.ValueKind == JsonValueKind.Array)
            {
                state.Rooms.Clear();
                foreach (var roomElement in roomsElement.EnumerateArray())
                {
                    state.Rooms.Add(ToLobbyRoom(roomElement));
                }

                state.Status = $"Loaded {state.Rooms.Count} rooms.";
                return null;
            }

            if (operation.Equals("createroom", StringComparison.OrdinalIgnoreCase) &&
                message.TryGetProperty("data", out var createData) &&
                createData.TryGetProperty("room", out var createdRoomElement))
            {
                var room = ToLobbyRoom(createdRoomElement);
                ReplaceRoom(state, room);
                state.Status = $"Created room '{room.RoomName}'.";
                return room;
            }

            if (message.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("error", out var errorElement) &&
                errorElement.ValueKind == JsonValueKind.String)
            {
                state.Status = errorElement.GetString() ?? "Operation failed.";
            }
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

    private static LobbyRoom ToLobbyRoom(JsonElement element)
    {
        var players = new List<string>();
        if (element.TryGetProperty("players", out var playersElement) && playersElement.ValueKind == JsonValueKind.Array)
        {
            players.AddRange(playersElement.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty));
        }

        return new LobbyRoom
        {
            RoomId = ReadString(element, "roomId"),
            GameId = ReadString(element, "gameId"),
            RoomName = ReadString(element, "roomName"),
            GameEndpoint = ReadString(element, "gameEndpoint"),
            Players = players
        };
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return TryReadString(element, propertyName, out var value) ? value : string.Empty;
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

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
