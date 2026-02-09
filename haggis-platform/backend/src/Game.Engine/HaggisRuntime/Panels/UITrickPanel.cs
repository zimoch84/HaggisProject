using Haggis.Interfaces;
using Haggis.Model;
using System.Text;

public class UITrickPanel : PanelRegionBase
{
    public UITrickPanel(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    override public void ApplyTextBuffer(HaggisGameState state)
    {
        TextBuffer.Clear();
        foreach (var action in state.CurrentTrickPlay.Actions)
        {
            string playerName = action.PlayerName;
            string actionText= action.Desc;
            ConsoleColor color = GetPlayerColor(action?.Player, state);
            string full = $"{playerName}: {actionText}; ";
            TextBuffer.WriteLine(full, color);

        }
    }

    private ConsoleColor GetPlayerColor(IHaggisPlayer player, HaggisGameState state)
    {
        var palette = new[]
        {
            ConsoleColor.Cyan,
            ConsoleColor.Green,
            ConsoleColor.Yellow,
            ConsoleColor.Magenta,
            ConsoleColor.Blue,
            ConsoleColor.Red,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkYellow
        };

        if (player != null && state?.Players != null)
        {
            try
            {
                for (int i = 0; i < state.Players.Count; i++)
                {
                    var p = state.Players[i];
                    if (p == null) continue;
                    if (player.GUID == p.GUID) return palette[i % palette.Length];
                    if (!string.IsNullOrEmpty(player.Name) && player.Name == p.Name) return palette[i % palette.Length];
                }
            }
            catch { /* ignore */ }
        }

        string key = player?.Name ?? (player?.GetHashCode().ToString() ?? "unknown");
        int hash = 0;
        foreach (var ch in key) hash = (hash * 31) + ch;
        int idx = Math.Abs(hash) % palette.Length;
        return palette[idx];
    }
}
