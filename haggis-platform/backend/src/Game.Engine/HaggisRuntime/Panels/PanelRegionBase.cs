using Haggis.Model;
using System.Collections.Generic;

public abstract class PanelRegionBase
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    private readonly string _header;

    public bool IsVisible { get; set; }

    public TextBuffer TextBuffer { get; }

    public PanelRegionBase(string header, int x, int y, int width, int height)
    {   
        TextBuffer = new TextBuffer(Math.Max(0, width - 2), Math.Max(0, height - 2));
        _header = header;
        X = Math.Max(0, x);
        Y = Math.Max(0, y);
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
    }

    public int Right => X + Width - 1;
    public int Bottom => Y + Height - 1;

    private void SafeWrite(int x, int y, string text)
    {
        if (text is null)
            return;

        int relX = System.Math.Max(0, x);
        int relY = System.Math.Max(0, y);

        // Jeśli start poza regionem - nic do zrobienia
        if (relY >= Height || relX >= Width)
            return;

        int absX = X + relX;
        int absY = Y + relY;

        // Przytnij tekst, by nie wychodził poza prawą krawędź regionu
        int maxLen = Width - relX;
        string outText = text.Length <= maxLen ? text : text.Substring(0, maxLen);

        try
        {
            // Dodatkowe zabezpieczenie przed ujemnymi globalnymi współrzędnymi
            if (absX < 0 || absY < 0)
                return;

            Console.SetCursorPosition(absX, absY);
            Console.Write(outText);
        }
        catch
        {
            // Celowo ignorujemy wyjątki związane z pozycjonowaniem kursora lub IO,
            // aby metoda pozostała "bezpieczna" i nie przerywała działania programu.
        }
    }

    private void DrawBorder()
    {
        if (Width <= 0 || Height <= 0)
            return;

        // Build top border in one write
        var topSb = new System.Text.StringBuilder();
        topSb.Append('┌');
        if (Width > 2)
        {
            for (int i = 0; i < Width - 2; i++) topSb.Append('─');
        }
        if (Width > 1) topSb.Append('┐');

        SafeWrite(0, 0, topSb.ToString());

        // Build bottom border in one write
        var bottomSb = new System.Text.StringBuilder();
        bottomSb.Append('└');
        if (Width > 2)
        {
            for (int i = 0; i < Width - 2; i++) bottomSb.Append('─');
        }
        if (Width > 1) bottomSb.Append('┘');

        SafeWrite(0, Height - 1, bottomSb.ToString());

        // Vertical edges - write as single char per side but avoid setting cursor per cell by building columns
        for (int y = 1; y < Height - 1; y++)
        {
            SafeWrite(0, y, "│");
            SafeWrite(Width - 1, y, "│");
        }

        // Wstaw nagłówek (jeśli istnieje) w górną krawędź, wyśrodkowany pomiędzy narożnikami
        if (!string.IsNullOrEmpty(_header))
        {
            int innerWidth = Width - 2;
            if (innerWidth > 0)
            {
                // Dodaj spacje dookoła headera dla lepszego wyglądu
                string hdr = " " + _header + " ";
                if (hdr.Length > innerWidth)
                    hdr = hdr.Substring(0, innerWidth);

                int startInner = 1 + (innerWidth - hdr.Length) / 2;
                SafeWrite(startInner, 0, hdr);
            }
        }
    }

    private void Draw()
    {
        DrawBorder();

        var prevColor = Console.ForegroundColor;

        // Rysujemy zawartość TextBuffer przesuniętą o 1x,1y aby nie nadpisywać ramki/nagłówka
        int maxRow = TextBuffer.Height;
        for (int y = 0; y < maxRow; y++)
        {
            int absY = Y + 1 + y; // przesunięcie w dół o 1 (poza górną ramkę)
            // Jeśli poza dolną granicą lub ujemne - pomiń
            if (absY < 0 || absY > Bottom - 1)
                continue;

            // Get batched segments for this row
            var segments = TextBuffer.GetRowSegments(y);
            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg.Text)) continue;
                int absX = X + 1 + seg.X; // przesunięcie w prawo o 1 (poza lewą ramkę)
                if (absX < 0 || absX > Right - 1) continue;

                try
                {
                    Console.SetCursorPosition(absX, absY);
                    Console.ForegroundColor = seg.Color ?? prevColor;
                    Console.Write(seg.Text);
                }
                catch
                {
                    // ignorujemy błędy pozycji kursora/IO
                }
            }
        }

        Console.ForegroundColor = prevColor;
    }

    public void DrawState(HaggisGameState state)
    {
        ApplyTextBuffer(state);
        if (IsVisible)
        {
            Draw();
        }
    }

    public abstract void ApplyTextBuffer(HaggisGameState state);

}