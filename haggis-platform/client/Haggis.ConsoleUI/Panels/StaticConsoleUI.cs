using Haggis.Domain.Model;
using Haggis.AI.Strategies;
using Haggis.AI.Model;

public class StaticConsoleUI
{
    private readonly int W;
    private readonly int H;
    private readonly List<PanelRegionBase> panels = new();
    private string[]? _lastFrame;

    public StaticConsoleUI()
    {
        W = SafeConsoleWidth();
        H = SafeConsoleHeight();
    }

    public StaticConsoleUI(RoundState state)
    {
        W = SafeConsoleWidth();
        H = SafeConsoleHeight();
        Initialize(state);
    }

    private static int SafeConsoleWidth()
    {
        try
        {
            return Math.Max(120, Console.WindowWidth);
        }
        catch
        {
            return 120;
        }
    }

    private static int SafeConsoleHeight()
    {
        try
        {
            return Math.Max(30, Console.WindowHeight);
        }
        catch
        {
            return 30;
        }
    }

    private static void SafeClear()
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // Output can be redirected or console unavailable.
        }
    }

    private static void SafeSetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch
        {
            // Ignore when terminal does not support cursor control.
        }
    }

    private void Initialize(RoundState state)
    {
        SafeClear();
        SafeSetCursorVisible(false);

        int totalWidth = Math.Max(100, W);
        int totalHeight = Math.Max(28, H);

        int playersHeight = 11;
        int inputHeight = 3;
        int bodyHeight = Math.Max(8, totalHeight - playersHeight - inputHeight);

        int actionsWidth = Math.Max(32, totalWidth / 3);
        int leftWidth = totalWidth - actionsWidth;
        int trickWidth = Math.Max(30, leftWidth / 2);
        int debugWidth = Math.Max(30, leftWidth - trickWidth);

        int scoreWidth = Math.Min(40, Math.Max(24, totalWidth - 6));
        int scoreHeight = Math.Min(9, Math.Max(5, totalHeight - 6));
        int scoreX = Math.Max(0, (totalWidth - scoreWidth) / 2);
        int scoreY = Math.Max(0, (totalHeight - scoreHeight) / 2 - 1);

        panels.AddRange(new PanelRegionBase[]
        {
            new UIPlayersPanel("Players", 0, 0, leftWidth, playersHeight),
            new UITrickPanel("Table", 0, playersHeight, trickWidth, bodyHeight),
            new UIDebugPanel("Debug", trickWidth, playersHeight, debugWidth, bodyHeight),
            new UIPossibleActionPanel("Actions", leftWidth, 0, actionsWidth, totalHeight - inputHeight),
            new UIScoringPanel("Score", scoreX, scoreY, scoreWidth, scoreHeight),
            new UIInput("Input", 0, totalHeight - inputHeight, totalWidth, inputHeight)
        });

        state.Players
            .OfType<AIPlayer>()
            .ToList()
            .ForEach(p =>
            {
                if (p.PlayStrategy is MonteCarloStrategy mcStrategy)
                {
                    var debugPanel = panels.OfType<UIDebugPanel>().FirstOrDefault();
                    if (debugPanel != null)
                    {
                        debugPanel.Attach(mcStrategy);
                        mcStrategy.OnComputed += debugPanel.Handle;
                    }
                }
            });
    }

    public void Render(RoundState state)
    {
        SafeClear();

        foreach (var panel in panels)
        {
            panel.DrawState(state);
        }
    }

    public int ReadHumanInput(RoundState state)
    {
        var uiInput = panels.OfType<UIInput>().FirstOrDefault();
        if (uiInput != null)
            return uiInput.ReadHumanInput(state);

        return -1;
    }

    public void RenderRemote(
        RemoteGameState? state,
        string playerId,
        string gameId,
        string status,
        IReadOnlyList<string> roomPlayers,
        bool autoStart,
        string inputText = "")
    {
        SafeSetCursorVisible(false);
        var frame = CreateFrame();
        var row = 0;

        WriteHeader(frame, ref row, $"Haggis WS | game={gameId} | player={playerId}");
        WriteLine(frame, ref row, $"Status: {status}");
        WriteLine(frame, ref row, $"Room players: {(roomPlayers.Count == 0 ? "(none)" : string.Join(", ", roomPlayers))}");

        if (state is null)
        {
            WriteLine(frame, ref row, string.Empty);
            WriteLine(frame, ref row, "No game state yet.");
            WriteLine(frame, ref row, autoStart
                ? "Auto-start is enabled. Waiting for at least 2 joined players."
                : "Use /start to initialize the game or /back to return to lobby.");
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                WriteLine(frame, ref row, $"Input: {inputText}");
            }
            FlushFrame(frame);
            return;
        }

        WriteLine(frame, ref row, string.Empty);
        WriteLine(frame, ref row, $"State version: {state.Version}");
        WriteLine(frame, ref row, $"Current player: {state.CurrentPlayerId}");
        WriteLine(frame, ref row, $"Round over: {state.RoundOver}");

        if (state.AppliedMove is not null)
        {
            WriteLine(frame, ref row, $"Last move: {state.AppliedMove.PlayerId} -> {state.AppliedMove.Action}");
        }

        WriteLine(frame, ref row, string.Empty);
        WriteHeader(frame, ref row, "Players");
        foreach (var player in state.Players)
        {
            var marker = player.Id.Equals(state.CurrentPlayerId, StringComparison.OrdinalIgnoreCase) ? "*" : " ";
            var finished = player.Finished ? " finished" : string.Empty;
            WriteLine(frame, ref row, $"{marker} {player.Id}: score={player.Score}, hand={player.HandCount}{finished}");
            WriteLine(frame, ref row, $"  cards: {FormatCards(player.Hand)}");
        }

        WriteLine(frame, ref row, string.Empty);
        WriteHeader(frame, ref row, "Table");
        if (state.Trick.Count == 0)
        {
            WriteLine(frame, ref row, "(empty)");
        }
        else
        {
            foreach (var action in state.Trick)
            {
                WriteLine(frame, ref row, $"{action.PlayerId}: {action.Description}");
            }
        }

        WriteLine(frame, ref row, string.Empty);
        WriteHeader(frame, ref row, "Possible Actions");
        if (state.PossibleActions.Count == 0)
        {
            WriteLine(frame, ref row, "(waiting)");
        }
        else
        {
            for (var i = 0; i < state.PossibleActions.Count; i++)
            {
                var action = state.PossibleActions[i];
                WriteLine(frame, ref row, $"[{i}] {action.Type} {action.Action}".TrimEnd());
            }
        }

        WriteLine(frame, ref row, string.Empty);
        WriteLine(frame, ref row, state.CurrentPlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase)
            ? "Your turn."
            : "Waiting for server update.");

        if (state.CurrentPlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase))
        {
            WriteLine(frame, ref row, $"Input: {inputText}");
        }

        FlushFrame(frame);
    }

    public void RenderLogin()
    {
        SafeSetCursorVisible(false);
        var layout = CreateStandardLayout();
        var frame = CreateFrame();

        DrawWindow(frame, layout.FullX, layout.FullY, layout.FullWidth, layout.FullHeight, "Haggis ConsoleUI");
        DrawWindow(frame, layout.LeftX, layout.TopY, layout.LeftWidth, layout.TopHeight, "Login");
        DrawWindow(frame, layout.RightX, layout.TopY, layout.RightWidth, layout.TopHeight, "Info");
        DrawWindow(frame, layout.LeftX, layout.BodyY, layout.LeftWidth, layout.BodyHeight, "Welcome");
        DrawWindow(frame, layout.RightX, layout.BodyY, layout.RightWidth, layout.BodyHeight, "Tips");
        DrawWindow(frame, layout.FullX, layout.InputY, layout.FullWidth, layout.InputHeight, "Input");

        WriteLines(frame, layout.LeftX + 2, layout.TopY + 2, new[]
        {
            "Podaj nazwe uzytkownika",
            "i nacisnij Enter."
        });

        WriteLines(frame, layout.RightX + 2, layout.TopY + 2, new[]
        {
            "Tryb zdalny laczy sie z",
            "serwerem WebSocket i",
            "otwiera lobby gier."
        });

        WriteLines(frame, layout.LeftX + 2, layout.BodyY + 2, new[]
        {
            "Po zalogowaniu zobaczysz:",
            "- liste dostepnych gier",
            "- chat publiczny",
            "- opcje tworzenia pokoju",
            "- opcje dolaczania do pokoju"
        });

        WriteLines(frame, layout.RightX + 2, layout.BodyY + 2, new[]
        {
            "Mozesz pominac ten ekran",
            "uruchamiajac aplikacje z",
            "argumentem:",
            "remote --user=piotr"
        });

        ClearInputLine(frame, layout);
        WriteAt(frame, layout.FullX + 2, layout.InputY + 2, "User: ");
        FlushFrame(frame);
    }

    public string ReadLoginInput()
    {
        SafeSetCursorVisible(true);
        var layout = CreateStandardLayout();
        SafeMoveCursor(layout.FullX + 8, layout.InputY + 2);
        var input = Console.ReadLine() ?? string.Empty;
        SafeSetCursorVisible(false);
        return input;
    }

    public void RenderLobby(string playerId, LobbyState state, string inputText = "")
    {
        SafeSetCursorVisible(false);
        var layout = CreateStandardLayout();
        var frame = CreateFrame();

        DrawWindow(frame, layout.FullX, layout.FullY, layout.FullWidth, layout.FullHeight, $"Lobby | user={playerId}");
        DrawWindow(frame, layout.LeftX, layout.TopY, layout.LeftWidth, layout.TopHeight, "Games");
        DrawWindow(frame, layout.RightX, layout.TopY, layout.RightWidth, layout.TopHeight, "Public Chat");
        DrawWindow(frame, layout.LeftX, layout.BodyY, layout.LeftWidth, layout.BodyHeight, "Status");
        DrawWindow(frame, layout.RightX, layout.BodyY, layout.RightWidth, layout.BodyHeight, "Commands");
        DrawWindow(frame, layout.FullX, layout.InputY, layout.FullWidth, layout.InputHeight, "Input");

        var roomLines = new List<string>();
        if (state.Rooms.Count == 0)
        {
            roomLines.Add("(brak pokoi)");
        }
        else
        {
            for (var i = 0; i < state.Rooms.Count; i++)
            {
                var room = state.Rooms[i];
                var name = string.IsNullOrWhiteSpace(room.RoomName) ? room.RoomId : room.RoomName;
                roomLines.Add($"[{i}] {name}");
                roomLines.Add($"id={room.RoomId}");
                roomLines.Add($"players={string.Join(", ", room.Players)}");
                roomLines.Add(string.Empty);
            }
        }

        var chatLines = new List<string>();
        if (state.Messages.Count == 0)
        {
            chatLines.Add("(brak wiadomosci)");
        }
        else
        {
            chatLines.AddRange(state.Messages.Select(message => $"{message.PlayerId}: {message.Text}"));
        }

        WriteLines(frame, layout.LeftX + 2, layout.TopY + 2, roomLines, layout.TopInnerHeight, layout.LeftInnerWidth);
        WriteLines(frame, layout.RightX + 2, layout.TopY + 2, chatLines, layout.TopInnerHeight, layout.RightInnerWidth);
        WriteLines(frame, layout.LeftX + 2, layout.BodyY + 2, new[]
        {
            state.Status,
            string.Empty,
            "Pusty Enter odswieza liste gier."
        }, layout.BodyInnerHeight, layout.LeftInnerWidth);
        WriteLines(frame, layout.RightX + 2, layout.BodyY + 2, new[]
        {
            state.CurrentInputHint,
            string.Empty,
            "/create <name>",
            "/join <index|roomId>",
            "/chat <text>",
            "/refresh",
            "/quit"
        }, layout.BodyInnerHeight, layout.RightInnerWidth);

        ClearInputLine(frame, layout);
        WriteAt(frame, layout.FullX + 2, layout.InputY + 2, $"> {inputText}");
        FlushFrame(frame);
    }

    private static void DrawWindow(char[][] frame, int x, int y, int width, int height, string title)
    {
        if (width < 4 || height < 3)
        {
            return;
        }

        WriteAt(frame, x, y, "+" + new string('-', width - 2) + "+");
        for (var row = 1; row < height - 1; row++)
        {
            WriteAt(frame, x, y + row, "|");
            WriteAt(frame, x + width - 1, y + row, "|");
        }
        WriteAt(frame, x, y + height - 1, "+" + new string('-', width - 2) + "+");

        if (!string.IsNullOrWhiteSpace(title) && width > 4)
        {
            var label = $" {title} ";
            if (label.Length > width - 2)
            {
                label = label[..(width - 2)];
            }

            WriteAt(frame, x + 1 + Math.Max(0, (width - 2 - label.Length) / 2), y, label);
        }
    }

    private static void WriteLines(char[][] frame, int x, int y, IEnumerable<string> lines, int? maxHeight = null, int? maxWidth = null)
    {
        var row = 0;
        foreach (var line in lines)
        {
            if (maxHeight.HasValue && row >= maxHeight.Value)
            {
                break;
            }

            var text = line ?? string.Empty;
            if (maxWidth.HasValue && text.Length > maxWidth.Value)
            {
                text = text[..maxWidth.Value];
            }

            WriteAt(frame, x, y + row, text);
            row++;
        }
    }

    private static void WriteAt(char[][] frame, int x, int y, string text)
    {
        if (y < 0 || y >= frame.Length)
        {
            return;
        }

        var row = frame[y];
        for (var i = 0; i < text.Length; i++)
        {
            var targetX = x + i;
            if (targetX < 0 || targetX >= row.Length)
            {
                continue;
            }

            row[targetX] = text[i];
        }
    }

    private static void SafeMoveCursor(int x, int y)
    {
        try
        {
            Console.SetCursorPosition(Math.Max(0, x), Math.Max(0, y));
        }
        catch
        {
        }
    }

    private static void ClearInputLine(char[][] frame, StandardLayout layout)
    {
        var width = Math.Max(1, layout.FullWidth - 4);
        WriteAt(frame, layout.FullX + 2, layout.InputY + 2, new string(' ', width));
    }

    private StandardLayout CreateStandardLayout()
    {
        var fullWidth = Math.Max(80, W);
        var fullHeight = Math.Max(24, H);
        var outerX = 0;
        var outerY = 0;
        var topY = 1;
        var inputHeight = 5;
        var availableHeight = fullHeight - inputHeight - 1;
        var topHeight = Math.Max(10, availableHeight / 2);
        var bodyY = topY + topHeight;
        var bodyHeight = Math.Max(6, availableHeight - topHeight);
        var inputY = fullHeight - inputHeight;
        var leftWidth = fullWidth / 2;
        var rightWidth = fullWidth - leftWidth;

        return new StandardLayout(
            outerX,
            outerY,
            fullWidth,
            fullHeight,
            outerX,
            topY,
            leftWidth,
            rightWidth,
            bodyY,
            inputY,
            topHeight,
            bodyHeight,
            inputHeight);
    }

    private readonly record struct StandardLayout(
        int FullX,
        int FullY,
        int FullWidth,
        int FullHeight,
        int LeftX,
        int TopY,
        int LeftWidth,
        int RightWidth,
        int BodyY,
        int InputY,
        int TopHeight,
        int BodyHeight,
        int InputHeight)
    {
        public int RightX => LeftX + LeftWidth;
        public int LeftInnerWidth => Math.Max(1, LeftWidth - 4);
        public int RightInnerWidth => Math.Max(1, RightWidth - 4);
        public int TopInnerHeight => Math.Max(1, TopHeight - 4);
        public int BodyInnerHeight => Math.Max(1, BodyHeight - 4);
    }

    private static string FormatCards(IReadOnlyList<string> cards)
    {
        if (cards.Count == 0)
        {
            return "(none)";
        }

        return string.Join(" ", cards);
    }

    private static void WriteHeader(string title)
    {
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }

    private static void WriteHeader(char[][] frame, ref int row, string title)
    {
        WriteLine(frame, ref row, title);
        WriteLine(frame, ref row, new string('=', title.Length));
    }

    private static void WriteLine(char[][] frame, ref int row, string text)
    {
        if (row >= frame.Length)
        {
            return;
        }

        WriteAt(frame, 0, row, text);
        row++;
    }

    private char[][] CreateFrame()
    {
        var width = Math.Max(1, W);
        var height = Math.Max(1, H);
        var frame = new char[height][];
        for (var y = 0; y < height; y++)
        {
            frame[y] = Enumerable.Repeat(' ', width).ToArray();
        }

        return frame;
    }

    private void FlushFrame(char[][] frame)
    {
        var lines = frame.Select(chars => new string(chars)).ToArray();
        if (_lastFrame is null || _lastFrame.Length != lines.Length)
        {
            SafeClear();
            for (var y = 0; y < lines.Length; y++)
            {
                SafeMoveCursor(0, y);
                Console.Write(lines[y]);
            }

            _lastFrame = lines;
            return;
        }

        for (var y = 0; y < lines.Length; y++)
        {
            if (string.Equals(_lastFrame[y], lines[y], StringComparison.Ordinal))
            {
                continue;
            }

            SafeMoveCursor(0, y);
            Console.Write(lines[y]);
            _lastFrame[y] = lines[y];
        }
    }
}
