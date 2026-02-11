using System.Net.WebSockets;
using Serwer.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GlobalChatHub>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Serwer.API is running.");

app.Map("/ws/chat/global", async (HttpContext context, GlobalChatHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleClientAsync(socket, context.RequestAborted);
});

app.Run();
