using Haggis.Domain.Model;
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

        // Je�li start poza regionem - nic do zrobienia
        if (relY >= Height || relX >= Width)
            return;

        int absX = X + relX;
        int absY = Y + relY;

        // Przytnij tekst, by nie wychodzi� poza praw� kraw�d� regionu
        int maxLen = Width - relX;
        string outText = text.Length <= maxLen ? text : text.Substring(0, maxLen);

        try
        {
            // Dodatkowe zabezpieczenie przed ujemnymi globalnymi wsp�rz�dnymi
            if (absX < 0 || absY < 0)
                return;

            Console.SetCursorPosition(absX, absY);
            Console.Write(outText);
        }
        catch
        {
            // Celowo ignorujemy wyj�tki zwi�zane z pozycjonowaniem kursora lub IO,
            // aby metoda pozosta�a "bezpieczna" i nie przerywa�a dzia�ania programu.
        }
    }

    private void DrawBorder()
    {
        if (Width <= 0 || Height <= 0)
            return;

        // Build top border in one write
        var topSb = new System.Text.StringBuilder();
        topSb.Append('-');
        if (Width > 2)
        {
            for (int i = 0; i < Width - 2; i++) topSb.Append('�');
        }
        if (Width > 1) topSb.Append('�');

        SafeWrite(0, 0, topSb.ToString());

        // Build bottom border in one write
        var bottomSb = new System.Text.StringBuilder();
        bottomSb.Append('L');
        if (Width > 2)
        {
            for (int i = 0; i < Width - 2; i++) bottomSb.Append('�');
        }
        if (Width > 1) bottomSb.Append('-');

        SafeWrite(0, Height - 1, bottomSb.ToString());

        // Vertical edges - write as single char per side but avoid setting cursor per cell by building columns
        for (int y = 1; y < Height - 1; y++)
        {
            SafeWrite(0, y, "-");
            SafeWrite(Width - 1, y, "-");
        }

        // Wstaw nag��wek (je�li istnieje) w g�rn� kraw�d�, wy�rodkowany pomi�dzy naro�nikami
        if (!string.IsNullOrEmpty(_header))
        {
            int innerWidth = Width - 2;
            if (innerWidth > 0)
            {
                // Dodaj spacje dooko�a headera dla lepszego wygl�du
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

        // Rysujemy zawarto�� TextBuffer przesuni�t� o 1x,1y aby nie nadpisywa� ramki/nag��wka
        int maxRow = TextBuffer.Height;
        for (int y = 0; y < maxRow; y++)
        {
            int absY = Y + 1 + y; // przesuni�cie w d� o 1 (poza g�rn� ramk�)
            // Je�li poza doln� granic� lub ujemne - pomi�
            if (absY < 0 || absY > Bottom - 1)
                continue;

            // Get batched segments for this row
            var segments = TextBuffer.GetRowSegments(y);
            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg.Text)) continue;
                int absX = X + 1 + seg.X; // przesuni�cie w prawo o 1 (poza lew� ramk�)
                if (absX < 0 || absX > Right - 1) continue;

                try
                {
                    Console.SetCursorPosition(absX, absY);
                    Console.ForegroundColor = seg.Color ?? prevColor;
                    Console.Write(seg.Text);
                }
                catch
                {
                    // ignorujemy b��dy pozycji kursora/IO
                }
            }
        }

        Console.ForegroundColor = prevColor;
    }

    public void DrawState(RoundState state)
    {
        ApplyTextBuffer(state);
        if (IsVisible)
        {
            Draw();
        }
    }

    public abstract void ApplyTextBuffer(RoundState state);

}