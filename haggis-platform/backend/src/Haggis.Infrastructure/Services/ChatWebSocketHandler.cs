namespace Haggis.Infrastructure.Services;

public sealed class ChatWebSocketHandler
{
    private readonly GlobalChatHub _globalChatHub;

    public ChatWebSocketHandler(GlobalChatHub globalChatHub)
    {
        _globalChatHub = globalChatHub;
    }

    public async Task HandleGlobalChatAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await _globalChatHub.HandleClientAsync(socket, context.RequestAborted);
    }
}
