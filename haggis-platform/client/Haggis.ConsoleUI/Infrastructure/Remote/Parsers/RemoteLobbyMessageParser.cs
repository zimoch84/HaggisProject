using System.Text.Json;

internal static class RemoteLobbyMessageParser
{
    public static RemoteLobbyInboundMessageDto? Parse(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RemoteLobbyInboundMessageDto>(message.GetRawText(), RemoteJsonSerializer.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
