using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.ConsoleUI.Presentation.Panels;

public class UITrickPanel : PanelRegionBase, IRoundStatePanel
{
    private RoundState? _state;

    public UITrickPanel(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
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
        if (_state?.CurrentTrickPlay?.Actions is null)
        {
            return;
        }

        foreach (var action in _state.CurrentTrickPlay.Actions)
        {
            string playerName = action.PlayerName;
            string actionText = action.Desc;
            ConsoleColor color = action?.Player is not null
                ? GetPlayerColor(action.Player, _state)
                : ConsoleColor.Gray;
            string full = $"{playerName}: {actionText}; ";
            TextBuffer.WriteLine(full, color);
        }
    }

    private ConsoleColor GetPlayerColor(IHaggisPlayer? player, RoundState state)
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

        if (player != null && state.Players != null)
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
            catch
            {
            }
        }

        string key = string.IsNullOrEmpty(player?.Name) ? "unknown" : player.Name;
        int hash = 0;
        foreach (var ch in key) hash = (hash * 31) + ch;
        int idx = Math.Abs(hash) % palette.Length;
        return palette[idx];
    }
}
