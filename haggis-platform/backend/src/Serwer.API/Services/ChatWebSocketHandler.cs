using System.Net.WebSockets;
using Serwer.API.Services.GameRooms;

namespace Serwer.API.Services;

public sealed class ChatWebSocketHandler
{
    private readonly GlobalChatHub _globalChatHub;
    private readonly RoomChatHub _roomChatHub;
    private readonly IGameRoomStore _roomStore;

    public ChatWebSocketHandler(
        GlobalChatHub globalChatHub,
        RoomChatHub roomChatHub,
        IGameRoomStore roomStore)
    {
        _globalChatHub = globalChatHub;
        _roomChatHub = roomChatHub;
        _roomStore = roomStore;
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

    public async Task HandleRoomChatAsync(HttpContext context, string roomId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected.");
            return;
        }

        if (!_roomStore.TryGetRoom(roomId, out _))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Game room not found.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await _roomChatHub.HandleClientAsync(roomId, socket, context.RequestAborted);
    }
}
