using Haggis.Domain.Model;
using System.Text;

public class UIPlayersPanel : PanelRegionBase
{

    public UIPlayersPanel(string header, int x, int y, int width, int height) 
        : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    override public void ApplyTextBuffer(RoundState state)
    {
        TextBuffer.Clear();

        // Definicja 3 kolor�w dla graczy (b�d� si� cyklicznie powtarza�)
        var playerColors = new[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Magenta };
        int playerIndex = 0;

        foreach (var p in state.Players)
        {
            int handCount = p.Hand?.Count ?? 0;
            int discardCount = p.Discard?.Count ?? 0;
            bool isCurrent = state.CurrentPlayer != null && p.GUID == state.CurrentPlayer.GUID;

            // Je�li gracz jest aktualny, wyr�nij na zielono, w przeciwnym razie u�yj jednego z 3 kolor�w
            var colorForPlayer = isCurrent ? ConsoleColor.Green : playerColors[playerIndex % playerColors.Length];

            TextBuffer.WriteLine($"{p.Name}: {handCount} kart, {discardCount} odrzuconych", colorForPlayer);

            // Sekcja: R�ka
            var handLabel = "  R�ka: ";
            var handPad = new string(' ', handLabel.Length);
            var handLines = WrapTokens(CardTokens(p.Hand?.ToList() ?? new List<Card>()));
            foreach (var lineText in handLines)
            {
                TextBuffer.Write(handLabel, colorForPlayer);
                TextBuffer.WriteLine(lineText, colorForPlayer);
            }

            // Sekcja: Odrzucone
            var discardLabel = "  Odrzucone: ";
            var discardPad = new string(' ', discardLabel.Length);
            var discardLines = WrapTokens(CardTokens(p.Discard?.ToList() ?? new List<Card>()));
            foreach (var lineText in discardLines)
            {
                TextBuffer.Write(discardLabel, colorForPlayer);
                TextBuffer.WriteLine(lineText, colorForPlayer);
            }

            playerIndex++;
        }
    }

    // Prywatne pomocnicze metody:
    // Zwraca tokeny reprezentuj�ce karty; je�li brak kart, zwraca token "(brak)".
    private IEnumerable<string> CardTokens(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return new[] { "(brak)" };

        return cards.Select(c => c?.ToString() ?? "(?)");
    }

    // Zawija tokeny w linie tak, aby d�ugo�� ka�dej linii nie przekroczy�a szeroko�ci panelu.
    private IEnumerable<string> WrapTokens(IEnumerable<string> tokens)
    {
        var result = new List<string>();
        if (tokens == null)
        {
            result.Add(string.Empty);
            return result;
        }

        // Rezerwa szeroko�ci: zostawiamy kilka znak�w marginesu (np. 4)
        int maxLineWidth = Math.Max(10, this.Width - 4);

        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            var t = token ?? string.Empty;
            if (sb.Length == 0)
            {
                // Je�eli pojedynczy token jest d�u�szy ni� maxLineWidth, dopasuj go bez �amania
                sb.Append(t);
            }
            else
            {
                // Sprawd�, czy dodaj�c token (z separatorem ' ') nie przekroczymy limitu
                if (sb.Length + 1 + t.Length <= maxLineWidth)
                {
                    sb.Append(' ').Append(t);
                }
                else
                {
                    // Zako�cz bie��c� lini� i rozpocznij now� od tokena
                    result.Add(sb.ToString());
                    sb.Clear();
                    sb.Append(t);
                }
            }
        }

        if (sb.Length > 0)
            result.Add(sb.ToString());

        if (result.Count == 0)
            result.Add(string.Empty);

        return result;
    }
}