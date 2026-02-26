namespace Haggis.Infrastructure.Services.Models;

public sealed record GameChatMessage(
    string PlayerId,
    string Text);
