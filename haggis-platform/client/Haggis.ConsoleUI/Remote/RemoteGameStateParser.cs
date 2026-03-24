using System.Text.Json;

public static class RemoteGameStateParser
{
    public static RemoteGameState ParseSnapshot(JsonElement snapshot)
    {
        if (snapshot.ValueKind != JsonValueKind.Object)
        {
            return new RemoteGameState();
        }

        try
        {
            var dto = JsonSerializer.Deserialize<RemoteGameSnapshotDto>(snapshot.GetRawText(), RemoteJsonSerializer.Options);
            return Map(dto);
        }
        catch (JsonException)
        {
            return new RemoteGameState();
        }
    }

    internal static RemoteGameState ParseSnapshot(RemoteGameSnapshotDto? snapshot) => Map(snapshot);

    private static RemoteGameState Map(RemoteGameSnapshotDto? snapshot)
    {
        var data = snapshot?.Data;

        return new RemoteGameState
        {
            Version = snapshot?.Version ?? 0,
            CurrentPlayerId = data?.CurrentPlayerId ?? string.Empty,
            RoundOver = data?.RoundOver ?? false,
            Players = data?.Players?.Select(MapPlayer).ToList() ?? new List<RemotePlayerState>(),
            Trick = data?.Trick?.Select(MapTrickAction).ToList() ?? new List<RemoteTrickAction>(),
            PossibleActions = data?.PossibleActions?.Select(MapPossibleAction).ToList() ?? new List<RemotePossibleAction>(),
            AppliedMove = data?.AppliedMove is null ? null : MapAppliedMove(data.AppliedMove)
        };
    }

    private static RemotePlayerState MapPlayer(RemotePlayerStateDto player)
    {
        return new RemotePlayerState
        {
            Id = player.Id ?? string.Empty,
            Score = player.Score ?? 0,
            HandCount = player.HandCount ?? 0,
            Finished = player.Finished ?? false,
            Hand = player.Hand?.Where(card => card is not null).Select(card => card ?? string.Empty).ToList() ?? new List<string>()
        };
    }

    private static RemoteTrickAction MapTrickAction(RemoteTrickActionDto action)
    {
        return new RemoteTrickAction
        {
            PlayerId = action.PlayerId ?? string.Empty,
            IsPass = action.IsPass ?? false,
            Description = action.Description ?? string.Empty
        };
    }

    private static RemotePossibleAction MapPossibleAction(RemotePossibleActionDto action)
    {
        return new RemotePossibleAction
        {
            Type = action.Type ?? string.Empty,
            Action = action.Action ?? string.Empty
        };
    }

    private static RemoteAppliedMove MapAppliedMove(RemoteAppliedMoveDto move)
    {
        return new RemoteAppliedMove
        {
            PlayerId = move.PlayerId ?? string.Empty,
            IsPass = move.IsPass ?? false,
            Action = move.Action ?? string.Empty
        };
    }
}
