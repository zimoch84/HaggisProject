using Serwer.API.Dtos.GameRooms;
using Serwer.API.Services;
using Serwer.API.Services.GameRooms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GlobalChatHub>();
builder.Services.AddSingleton<IGameRoomStore, GameRoomStore>();
builder.Services.AddSingleton<RoomChatHub>();
builder.Services.AddSingleton<ChatWebSocketHandler>();
builder.Services.AddSingleton<GameRoomWebSocketHandler>();
builder.Services.AddSingleton<SessionWebSocketHandler>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () => "Serwer.API is running.");

app.Map("/ws/session/login", (HttpContext context, SessionWebSocketHandler handler) =>
    handler.HandleLoginAsync(context));

app.Map("/ws/rooms/create", (HttpContext context, GameRoomWebSocketHandler handler) =>
    handler.HandleCreateRoomAsync(context));

app.Map("/ws/rooms/list", (HttpContext context, GameRoomWebSocketHandler handler) =>
    handler.HandleListRoomsAsync(context));

app.Map("/ws/rooms/{roomId}/join", (HttpContext context, string roomId, GameRoomWebSocketHandler handler) =>
    handler.HandleJoinRoomAsync(context, roomId));

app.Map("/ws/chat/global", (HttpContext context, ChatWebSocketHandler handler) =>
    handler.HandleGlobalChatAsync(context));

app.Map("/ws/chat/rooms/{roomId}", (HttpContext context, string roomId, ChatWebSocketHandler handler) =>
    handler.HandleRoomChatAsync(context, roomId));

app.Run();

public partial class Program;

