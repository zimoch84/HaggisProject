using Haggis.Model;
using Haggis.Strategies;

public class StaticConsoleUI
{
    int W = Console.WindowWidth;
    int H = Console.WindowHeight;
    int leftWidth;
    private List<PanelRegionBase> panels = new List<PanelRegionBase>();

    public StaticConsoleUI(HaggisGameState state)
    {
        leftWidth = W / 2 + 30;
        Initialize(state);
    }
   
    private void Initialize(HaggisGameState state)
    {
        Console.Clear();
        Console.CursorVisible = false;

        panels.AddRange(new PanelRegionBase[]
        {
            new UIPlayersPanel("Players", 0, 0, 114, 11),
            new UITrickPanel("Table",     0, 11, 114 / 2 , 40),
            new UIDebugPanel("Debug", leftWidth / 2, 11, 114 /2 , 40),
            new UIPossibleActionPanel("Actions", 114 + 1, 0, W - 114 - 2, H - 4),
            new UIScoringPanel(
                "Score",
                (W - Math.Min(40, Math.Max(10, W - 6))) / 2,
                (H - Math.Min(9, Math.Max(5, H - 6))) / 2 - 1,
                Math.Min(40, Math.Max(10, W - 6)),
                Math.Min(9, Math.Max(5, H - 6))),
            new UIInput("Input", 0, H - 3, W, 3)
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

    // -----------------------------------------------
    //                 MAIN RENDER
    // -----------------------------------------------

    public void Render(HaggisGameState state)
    {
        Console.Clear();

        foreach (var panel in panels)
        {
            panel.DrawState(state);
        }

    }

    public int ReadHumanInput(HaggisGameState state)
    {
        var uiInput = panels.OfType<UIInput>().FirstOrDefault();
        if (uiInput != null)
            return uiInput.ReadHumanInput(state);
        
        return -1;
    }


}
