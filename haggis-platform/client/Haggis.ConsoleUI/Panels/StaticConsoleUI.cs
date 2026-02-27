using Haggis.Domain.Model;
using Haggis.AI.Strategies;
using Haggis.AI.Model;

public class StaticConsoleUI
{
    private readonly int W;
    private readonly int H;
    private readonly int leftWidth;
    private List<PanelRegionBase> panels = new List<PanelRegionBase>();

    public StaticConsoleUI(RoundState state)
    {
        W = SafeConsoleWidth();
        H = SafeConsoleHeight();
        leftWidth = W / 2 + 30;
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
}

