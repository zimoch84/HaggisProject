using System.Text.Json;

public static class RemoteGameStateParser
{
    public static RemoteGameSnapshotDto ParseSnapshot(JsonElement snapshot)
    {
        if (snapshot.ValueKind != JsonValueKind.Object)
        {
            return new RemoteGameSnapshotDto();
        }

        try
        {
            return JsonSerializer.Deserialize<RemoteGameSnapshotDto>(snapshot.GetRawText(), RemoteJsonSerializer.Options)
                ?? new RemoteGameSnapshotDto();
        }
        catch (JsonException)
        {
            return new RemoteGameSnapshotDto();
        }
    }
}
