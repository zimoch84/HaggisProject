using Haggis.Domain.Model;
using Haggis.AI.Strategies;
using System;
using Haggis.AI.Model;

public class UIDebugPanel : PanelRegionBase
{
    public UIDebugPanel(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    public void Attach(MonteCarloStrategy strategy)
    {
        strategy.OnComputed += Handle;
    }

    public void Handle(MonteCarloResult result)
    {
        TextBuffer.WriteLine($"AI: {result.Player.Name}");

        foreach (var a in result.Actions)
        {
            double ratio = a.NumRuns == 0 ? 0 : (double)a.NumWins / a.NumRuns;
            TextBuffer.WriteLine($"{a.Action}  {a.NumWins}/{a.NumRuns} ({ratio:0.000})");
        }

    }

    public override void ApplyTextBuffer(HaggisGameState state)
    {
    }
}

