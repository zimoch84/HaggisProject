using System.Text.Json;

public sealed class RemoteGameState
{
    public long Version { get; set; }
    public string CurrentPlayerId { get; set; } = string.Empty;
    public bool RoundOver { get; set; }
    public List<RemotePlayerState> Players { get; set; } = new();
    public List<RemoteTrickAction> Trick { get; set; } = new();
    public List<RemotePossibleAction> PossibleActions { get; set; } = new();
    public RemoteAppliedMove? AppliedMove { get; set; }

    public static RemoteGameState FromSnapshot(JsonElement snapshot)
    {
        var state = new RemoteGameState();

        if (TryGetPropertyIgnoreCase(snapshot, "version", out var versionElement) && versionElement.TryGetInt64(out var version))
        {
            state.Version = version;
        }

        if (!TryGetPropertyIgnoreCase(snapshot, "data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return state;
        }

        if (TryGetPropertyIgnoreCase(data, "currentPlayerId", out var currentPlayerIdElement) &&
            currentPlayerIdElement.ValueKind == JsonValueKind.String)
        {
            state.CurrentPlayerId = currentPlayerIdElement.GetString() ?? string.Empty;
        }

        if (TryGetPropertyIgnoreCase(data, "roundOver", out var roundOverElement) &&
            roundOverElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            state.RoundOver = roundOverElement.GetBoolean();
        }

        if (TryGetPropertyIgnoreCase(data, "players", out var playersElement) && playersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var playerElement in playersElement.EnumerateArray())
            {
                state.Players.Add(new RemotePlayerState
                {
                    Id = ReadString(playerElement, "id"),
                    Score = ReadInt(playerElement, "score"),
                    HandCount = ReadInt(playerElement, "handCount"),
                    Finished = ReadBool(playerElement, "finished"),
                    Hand = ReadStringArray(playerElement, "hand")
                });
            }
        }

        if (TryGetPropertyIgnoreCase(data, "trick", out var trickElement) && trickElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var actionElement in trickElement.EnumerateArray())
            {
                state.Trick.Add(new RemoteTrickAction
                {
                    PlayerId = ReadString(actionElement, "playerId"),
                    IsPass = ReadBool(actionElement, "isPass"),
                    Description = ReadString(actionElement, "desc")
                });
            }
        }

        if (TryGetPropertyIgnoreCase(data, "possibleActions", out var possibleActionsElement) &&
            possibleActionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var actionElement in possibleActionsElement.EnumerateArray())
            {
                state.PossibleActions.Add(new RemotePossibleAction
                {
                    Type = ReadString(actionElement, "type"),
                    Action = ReadString(actionElement, "action")
                });
            }
        }

        if (TryGetPropertyIgnoreCase(data, "appliedMove", out var appliedMoveElement) && appliedMoveElement.ValueKind == JsonValueKind.Object)
        {
            state.AppliedMove = new RemoteAppliedMove
            {
                PlayerId = ReadString(appliedMoveElement, "playerId"),
                IsPass = ReadBool(appliedMoveElement, "isPass"),
                Action = ReadString(appliedMoveElement, "action")
            };
        }

        return state;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property) &&
            property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return property.GetBoolean();
        }

        return false;
    }

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        var values = new List<string>();
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                values.Add(item.GetString() ?? string.Empty);
            }
        }

        return values;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}

public sealed class RemotePlayerState
{
    public string Id { get; init; } = string.Empty;
    public int Score { get; init; }
    public int HandCount { get; init; }
    public bool Finished { get; init; }
    public List<string> Hand { get; init; } = new();
}

public sealed class RemoteTrickAction
{
    public string PlayerId { get; init; } = string.Empty;
    public bool IsPass { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class RemotePossibleAction
{
    public string Type { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
}

public sealed class RemoteAppliedMove
{
    public string PlayerId { get; init; } = string.Empty;
    public bool IsPass { get; init; }
    public string Action { get; init; } = string.Empty;
}
