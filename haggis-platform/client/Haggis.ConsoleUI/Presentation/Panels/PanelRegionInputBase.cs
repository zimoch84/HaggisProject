using System.Text;

public abstract class PanelRegionInputBase : PanelRegionBase
{
    private string _prompt = string.Empty;
    private readonly StringBuilder _buffer = new();
    private string? _submitted;

    protected PanelRegionInputBase(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
        IsVisible = true;
    }

    protected string Prompt => _prompt;
    protected string BufferText => _buffer.ToString();

    public void SetPrompt(string prompt)
    {
        _prompt = prompt ?? string.Empty;
    }

    public void Clear()
    {
        _buffer.Clear();
        _submitted = null;
    }

    public string CurrentText => _buffer.ToString();

    public void SetText(string text)
    {
        _buffer.Clear();
        _buffer.Append(text ?? string.Empty);
        _submitted = null;
    }

    public void HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                _submitted = _buffer.ToString();
                _buffer.Clear();
                break;
            case ConsoleKey.Backspace:
                if (_buffer.Length > 0)
                {
                    _buffer.Length--;
                }
                break;
            default:
                if (!char.IsControl(key.KeyChar))
                {
                    _buffer.Append(key.KeyChar);
                }
                break;
        }
    }

    public bool TryReadKey(out ConsoleKeyInfo key)
    {
        if (Console.KeyAvailable)
        {
            key = Console.ReadKey(intercept: true);
            return true;
        }

        key = default;
        return false;
    }

    public bool TryConsumeSubmitted(out string command)
    {
        if (_submitted is null)
        {
            command = string.Empty;
            return false;
        }

        command = _submitted;
        _submitted = null;
        return true;
    }

    public abstract IInputAction ParseCommand(string command);

    public override void ApplyTextBuffer()
    {
        TextBuffer.Clear();
        TextBuffer.Write(Prompt);
        TextBuffer.Write(BufferText);
    }
}
