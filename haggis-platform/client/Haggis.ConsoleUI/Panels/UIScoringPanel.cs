using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

public class UIScoringPanel : PanelRegionBase
{
    public UIScoringPanel(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
    {
        IsVisible = false;
    }

    override public void ApplyTextBuffer(RoundState state)
    {

        try
        {
            if (!state.RoundOver())
            {
                 return;
            }

            IsVisible = true;

            // zawarto��: lista graczy i wyniki (w TextBuffer zapisujemy zawarto�� wn�trza regionu)
            int maxLines = TextBuffer.Height;
            var players = state?.Players ?? Enumerable.Empty<IHaggisPlayer>();
            int line = 0;
            foreach (var p in players)
            {
                if (line >= maxLines) break;
                string name = p?.Name ?? "<brak>";
                string score = "Score: " + (p != null ? p.Score.ToString() : "0");
                string handCount = "";
                try
                {
                    handCount = p?.Hand != null ? $" (karty: {p.Hand.Count})" : "";
                }
                catch { handCount = ""; }

                string text = $"{name}: {score}{handCount}";
                int contentWidth = Math.Max(0, TextBuffer.Width);
                if (text.Length > contentWidth) text = text.Substring(0, Math.Max(0, contentWidth - 3)) + "...";
                TextBuffer.WriteLine(text.PadRight(Math.Max(0, contentWidth)));
                line++;
            }

            // je�li brak graczy to info
            if (!players.Any())
            {
                TextBuffer.WriteLine("Brak graczy".PadRight(Math.Max(0, TextBuffer.Width)));
            }
            
        }
        catch
        {
            // bezpieczne ignorowanie b��d�w rysowania
        }
    }
}