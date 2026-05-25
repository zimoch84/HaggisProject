using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.ConsoleUI.Presentation.Panels;

public class UIScoringPanel : PanelRegionBase, IRoundStatePanel
{
    private RoundState? _state;

    public UIScoringPanel(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
    {
        IsVisible = false;
    }

    public void SetState(RoundState state)
    {
        _state = state;
    }

    public override void ApplyTextBuffer()
    {
        TextBuffer.Clear();
        IsVisible = false;

        try
        {
            if (_state is null || !_state.RoundOver())
            {
                return;
            }

            IsVisible = true;

            int maxLines = TextBuffer.Height;
            var players = _state.Players ?? Enumerable.Empty<IHaggisPlayer>();
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
                catch
                {
                    handCount = "";
                }

                string text = $"{name}: {score}{handCount}";
                int contentWidth = Math.Max(0, TextBuffer.Width);
                if (text.Length > contentWidth) text = text.Substring(0, Math.Max(0, contentWidth - 3)) + "...";
                TextBuffer.WriteLine(text.PadRight(Math.Max(0, contentWidth)));
                line++;
            }

            if (!players.Any())
            {
                TextBuffer.WriteLine("Brak graczy".PadRight(Math.Max(0, TextBuffer.Width)));
            }
        }
        catch
        {
            // bezpieczne ignorowanie bledow rysowania
        }
    }
}
