using System.Text.Json;

internal static class RemoteGameMessageParser
{
    public static RemoteGameInboundMessageDto? Parse(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RemoteGameInboundMessageDto>(message.GetRawText(), RemoteJsonSerializer.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
