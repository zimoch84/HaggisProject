using Haggis.Domain.Model;
using System.Text;

public class UIPlayersPanel : PanelRegionBase
{

    public UIPlayersPanel(string header, int x, int y, int width, int height) 
        : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    override public void ApplyTextBuffer(HaggisGameState state)
    {
        TextBuffer.Clear();

        // Definicja 3 kolorów dla graczy (bêd¹ siê cyklicznie powtarzaæ)
        var playerColors = new[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Magenta };
        int playerIndex = 0;

        foreach (var p in state.Players)
        {
            int handCount = p.Hand?.Count ?? 0;
            int discardCount = p.Discard?.Count ?? 0;
            bool isCurrent = state.CurrentPlayer != null && p.GUID == state.CurrentPlayer.GUID;

            // Jeœli gracz jest aktualny, wyró¿nij na zielono, w przeciwnym razie u¿yj jednego z 3 kolorów
            var colorForPlayer = isCurrent ? ConsoleColor.Green : playerColors[playerIndex % playerColors.Length];

            TextBuffer.WriteLine($"{p.Name}: {handCount} kart, {discardCount} odrzuconych", colorForPlayer);

            // Sekcja: Rêka
            var handLabel = "  Rêka: ";
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
    // Zwraca tokeny reprezentuj¹ce karty; jeœli brak kart, zwraca token "(brak)".
    private IEnumerable<string> CardTokens(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return new[] { "(brak)" };

        return cards.Select(c => c?.ToString() ?? "(?)");
    }

    // Zawija tokeny w linie tak, aby d³ugoœæ ka¿dej linii nie przekroczy³a szerokoœci panelu.
    private IEnumerable<string> WrapTokens(IEnumerable<string> tokens)
    {
        var result = new List<string>();
        if (tokens == null)
        {
            result.Add(string.Empty);
            return result;
        }

        // Rezerwa szerokoœci: zostawiamy kilka znaków marginesu (np. 4)
        int maxLineWidth = Math.Max(10, this.Width - 4);

        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            var t = token ?? string.Empty;
            if (sb.Length == 0)
            {
                // Je¿eli pojedynczy token jest d³u¿szy ni¿ maxLineWidth, dopasuj go bez ³amania
                sb.Append(t);
            }
            else
            {
                // SprawdŸ, czy dodaj¹c token (z separatorem ' ') nie przekroczymy limitu
                if (sb.Length + 1 + t.Length <= maxLineWidth)
                {
                    sb.Append(' ').Append(t);
                }
                else
                {
                    // Zakoñcz bie¿¹c¹ liniê i rozpocznij now¹ od tokena
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