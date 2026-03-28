public sealed class UITextPanel : PanelRegionBase
{
    private IReadOnlyList<string> _lines = Array.Empty<string>();

    public UITextPanel(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    public void SetLines(IEnumerable<string> lines)
    {
        _lines = lines.ToList();
    }

    public override void ApplyTextBuffer()
    {
        TextBuffer.Clear();
        foreach (var line in _lines)
        {
            TextBuffer.WriteLine(line ?? string.Empty);
        }
    }
}
