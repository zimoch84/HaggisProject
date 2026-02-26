using Haggis.Domain.Model;
using Haggis.AI.Model;

public class UIPossibleActionPanel : PanelRegionBase
{
    public UIPossibleActionPanel(string header,int x, int y, int width, int height)
        : base(header, x, y, width, height) 
    {
        IsVisible = true;
    }

    override public void ApplyTextBuffer(HaggisGameState state)
    {

        if (state.CurrentPlayer is AIPlayer)
            return;

        TextBuffer.Clear();

        IList<HaggisAction> actions = state.PossibleActions;


        int totalWidth = Math.Max(1, TextBuffer.Width);
        int leftWidth = totalWidth / 2;
        int rightWidth = totalWidth - leftWidth;

        // Przygotuj listy linii dla lewej i prawej kolumny
        var leftLines = new List<string>();
        var rightLines = new List<string>();

        if (actions == null || actions.Count == 0)
        {
            leftLines.Add("Possible Actions:");
            leftLines.Add("(none)");
        }
        else
        {
            // wypełnij lewą kolumnę do wysokości, potem resztę do prawej
            int idx = 0;
            leftLines.Add("Possible Actions:");
            while (idx < actions.Count && leftLines.Count < TextBuffer.Height)
            {
                var s = $"[{idx}] {actions[idx]}";
                leftLines.Add(Truncate(s, leftWidth));
                idx++;
            }

            if (idx < actions.Count)
            {
                rightLines.Add("Possible Actions:");
                while (idx < actions.Count && rightLines.Count < TextBuffer.Height)
                {
                    var s = $"[{idx}] {actions[idx]}";
                    rightLines.Add(Truncate(s, rightWidth));
                    idx++;
                }
            }
        }

        // Zbuduj finalne wiersze łącząc kolumny
        int maxLines = Math.Max(leftLines.Count, rightLines.Count);
        for (int i = 0; i < maxLines && i < TextBuffer.Height; i++)
        {
            string left = i < leftLines.Count ? leftLines[i] : string.Empty;
            string right = i < rightLines.Count ? rightLines[i] : string.Empty;
            string line = left.PadRight(leftWidth).Substring(0, leftWidth) + right.PadRight(rightWidth).Substring(0, rightWidth);
            TextBuffer.WriteLine(line);
        }
    }

    private string Truncate(string s, int w)
    {
        if (s == null) s = string.Empty;
        if (w <= 0) return string.Empty;
        if (s.Length <= w) return s;
        return s.Substring(0, w);
    }
}


