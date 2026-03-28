using Haggis.Domain.Model;
using Haggis.AI.Model;

public class UIPossibleActionPanel : PanelRegionBase, IRoundStatePanel
{
    private RoundState? _state;

    public UIPossibleActionPanel(string header, int x, int y, int width, int height)
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
        if (_state is null)
        {
            return;
        }

        if (_state.CurrentPlayer is AIPlayer)
            return;

        IList<HaggisAction> actions = _state.PossibleActions;

        int totalWidth = Math.Max(1, TextBuffer.Width);
        int leftWidth = totalWidth / 2;
        int rightWidth = totalWidth - leftWidth;

        var leftLines = new List<string>();
        var rightLines = new List<string>();

        if (actions == null || actions.Count == 0)
        {
            leftLines.Add("Possible Actions:");
            leftLines.Add("(none)");
        }
        else
        {
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

        int maxLines = Math.Max(leftLines.Count, rightLines.Count);
        for (int i = 0; i < maxLines && i < TextBuffer.Height; i++)
        {
            string left = i < leftLines.Count ? leftLines[i] : string.Empty;
            string right = i < rightLines.Count ? rightLines[i] : string.Empty;
            string line = left.PadRight(leftWidth).Substring(0, leftWidth)
                + right.PadRight(rightWidth).Substring(0, rightWidth);
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
