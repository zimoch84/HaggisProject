using Haggis.ConsoleUI.Presentation.Panels;
using Haggis.ConsoleUI.Presentation.Panels.Inputs;
using Haggis.ConsoleUI.Presentation.ViewModels.Lobby;

namespace Haggis.ConsoleUI.Presentation.Screens;

public sealed class LobbyScreen : PanelScreenBase
{
    private readonly UITextPanel _gamesPanel;
    private readonly UITextPanel _chatPanel;
    private readonly UITextPanel _statusPanel;
    private readonly UITextPanel _commandsPanel;

    private string _playerId = string.Empty;
    private LobbyState _state = new();

    public LobbyScreen() : base(CreateInputPanel())
    {
        var width = SafeConsoleWidth();
        var height = SafeConsoleHeight();
        var leftWidth = width / 2;
        var rightWidth = width - leftWidth;
        var topHeight = Math.Max(10, (height - 5) / 2);
        var bodyHeight = Math.Max(6, height - topHeight - 5);
        var bodyY = 1 + topHeight;

        _gamesPanel = new UITextPanel("Games", 0, 1, leftWidth, topHeight);
        _chatPanel = new UITextPanel("Public Chat", leftWidth, 1, rightWidth, topHeight);
        _statusPanel = new UITextPanel("Status", 0, bodyY, leftWidth, bodyHeight);
        _commandsPanel = new UITextPanel("Commands", leftWidth, bodyY, rightWidth, bodyHeight);

        AddPanel(_gamesPanel);
        AddPanel(_chatPanel);
        AddPanel(_statusPanel);
        AddPanel(_commandsPanel);
    }

    public async Task<LobbyScreenAction> ShowAsync(
        string playerId,
        LobbyState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        _playerId = playerId;
        _state = state;
        InputPanel.Clear();
        LobbyScreenAction cancelAction = new LobbyScreenAction.Quit();
        return await ReadCommandCoreAsync(pumpMessagesAsync, cancellationToken, cancelAction);
    }

    protected override void PreparePanels()
    {
        _gamesPanel.SetLines(BuildRoomLines(_state));
        _chatPanel.SetLines(BuildChatLines(_state));
        _statusPanel.SetLines(new[]
        {
            $"User: {_playerId}",
            _state.Status,
            string.Empty,
            "Pusty Enter odswieza liste gier."
        });
        _commandsPanel.SetLines(new[]
        {
            _state.CurrentInputHint,
            string.Empty,
            "/create <name>",
            "/join <index|roomId>",
            "/chat <text>",
            "/refresh",
            "/quit"
        });
        InputPanel.SetPrompt("> ");
    }

    private static IReadOnlyList<string> BuildRoomLines(LobbyState state)
    {
        if (state.Rooms.Count == 0)
        {
            return new[] { "(brak pokoi)" };
        }

        var lines = new List<string>();
        for (var i = 0; i < state.Rooms.Count; i++)
        {
            var room = state.Rooms[i];
            var name = string.IsNullOrWhiteSpace(room.RoomName) ? room.RoomId : room.RoomName;
            lines.Add($"[{i}] {name}");
            lines.Add($"id={room.RoomId}");
            lines.Add($"players={string.Join(", ", room.Players)}");
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildChatLines(LobbyState state)
    {
        if (state.Messages.Count == 0)
        {
            return new[] { "(brak wiadomosci)" };
        }

        return state.Messages
            .Select(message => $"{message.PlayerId}: {message.Text}")
            .ToList();
    }

    private static LobbyInput CreateInputPanel()
    {
        return new LobbyInput("Input", 0, SafeConsoleHeight() - 5, SafeConsoleWidth(), 5);
    }

    private static int SafeConsoleWidth()
    {
        try
        {
            return Math.Max(40, Console.WindowWidth - 1);
        }
        catch
        {
            return 119;
        }
    }

    private static int SafeConsoleHeight()
    {
        try
        {
            return Math.Max(12, Console.WindowHeight - 1);
        }
        catch
        {
            return 29;
        }
    }
}
