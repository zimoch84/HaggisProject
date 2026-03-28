using Haggis.Domain.Model;
using System.Text;

public class UIPlayersPanel : PanelRegionBase, IRoundStatePanel
{
    private RoundState? _state;

    public UIPlayersPanel(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    public void SetState(RoundState state)
    {
        _state = state;
    }

    public override void ApplyTextBuffer()
    {
        TextBuffer.Clear();
        if (_state?.Players is null)
        {
            return;
        }

        var playerColors = new[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Magenta };
        int playerIndex = 0;

        foreach (var p in _state.Players)
        {
            int handCount = p.Hand?.Count ?? 0;
            int discardCount = p.Discard?.Count ?? 0;
            bool isCurrent = _state.CurrentPlayer != null && p.GUID == _state.CurrentPlayer.GUID;

            var colorForPlayer = isCurrent ? ConsoleColor.Green : playerColors[playerIndex % playerColors.Length];

            TextBuffer.WriteLine($"{p.Name}: {handCount} kart, {discardCount} odrzuconych", colorForPlayer);

            var handLabel = "  Reka: ";
            var handLines = WrapTokens(CardTokens(p.Hand?.ToList() ?? new List<Card>()));
            foreach (var lineText in handLines)
            {
                TextBuffer.Write(handLabel, colorForPlayer);
                TextBuffer.WriteLine(lineText, colorForPlayer);
            }

            var discardLabel = "  Odrzucone: ";
            var discardLines = WrapTokens(CardTokens(p.Discard?.ToList() ?? new List<Card>()));
            foreach (var lineText in discardLines)
            {
                TextBuffer.Write(discardLabel, colorForPlayer);
                TextBuffer.WriteLine(lineText, colorForPlayer);
            }

            playerIndex++;
        }
    }

    private IEnumerable<string> CardTokens(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return new[] { "(brak)" };

        return cards.Select(c => c?.ToString() ?? "(?)");
    }

    private IEnumerable<string> WrapTokens(IEnumerable<string> tokens)
    {
        var result = new List<string>();
        if (tokens == null)
        {
            result.Add(string.Empty);
            return result;
        }

        int maxLineWidth = Math.Max(10, Width - 4);

        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            var t = token ?? string.Empty;
            if (sb.Length == 0)
            {
                sb.Append(t);
            }
            else if (sb.Length + 1 + t.Length <= maxLineWidth)
            {
                sb.Append(' ').Append(t);
            }
            else
            {
                result.Add(sb.ToString());
                sb.Clear();
                sb.Append(t);
            }
        }

        if (sb.Length > 0)
            result.Add(sb.ToString());

        if (result.Count == 0)
            result.Add(string.Empty);

        return result;
    }
}
