using System;
using System.Text;
using System.Collections.Generic;

public class TextBuffer
{
    private readonly int _width;
    private readonly int _height;
    private readonly Cell[,] _cells;
    private int _cursorX;
    private int _cursorY;

    private struct Cell
    {
        public char Ch;
        public ConsoleColor? Color;
    }

    public int Width => _width;
    public int Height => _height;

    public TextBuffer(int width, int height)
    {
        _width = Math.Max(0, width);
        _height = Math.Max(0, height);
        _cells = new Cell[_height, _width];
        Clear();
    }

    public void Clear()
    {
        for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                _cells[y, x].Ch = ' ';
                _cells[y, x].Color = null;
            }

        _cursorX = 0;
        _cursorY = 0;
    }

    // Przesuniêcie zawartoœci bufora w górê o zadany amont wierszy
    private void ScrollUp(int lines)
    {
        if (lines <= 0) return;

        int move = Math.Min(lines, _height);
        // Przesuñ wiersze w górê
        for (int y = 0; y < _height - move; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _cells[y, x] = _cells[y + move, x];
            }
        }

        // Wyczyœæ dolne 'move' wierszy
        for (int y = _height - move; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _cells[y, x].Ch = ' ';
                _cells[y, x].Color = null;
            }
        }

        _cursorY -= move;
        if (_cursorY < 0) _cursorY = 0;
    }

    // Zapisuje tekst zaczynaj¹c od aktualnej pozycji kursora.
    // Podobne zachowanie do Console.Write: obs³uga '\n' i '\r'.
    public void Write(string text, ConsoleColor? color = null)
    {
        if (text is null) return;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (_cursorY > _height)
            {
                int excess = _cursorY - _height;
                ScrollUp(excess);
            }

            if (_cursorX > _width)
            {
                // zawijanie do nastêpnego wiersza
                _cursorX = 0;
                _cursorY++;
                if (_cursorY > _height)
                {
                    int excess = _cursorY - _height;
                    ScrollUp(excess);
                }
            }

            if (_cursorX < _width && _cursorY < _height)
            {
                _cells[_cursorY, _cursorX].Ch = c;
                _cells[_cursorY, _cursorX].Color = color;
            }

            _cursorX++;
        }
    }

    public void WriteLine(string text = "", ConsoleColor? color = null)
    {
        if (!string.IsNullOrEmpty(text))
            Write(text, color);

        _cursorX = 0;
        _cursorY++;
        if (_cursorY > _height)
        {
            int excess = _cursorY - _height;
            ScrollUp(excess);
        }
    }

    public bool TryGetCell(int x, int y, out char ch, out ConsoleColor? color)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
        {
            ch = default;
            color = default;
            return false;
        }

        ch = _cells[y, x].Ch;
        color = _cells[y, x].Color;
        return true;
    }

    public override string ToString()
    {
        if (_width == 0 || _height == 0)
            return string.Empty;

        var sb = new StringBuilder(_height * (_width + 1));
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
                if(_cells[y, x].Ch != ' ')
                    sb.Append(_cells[y, x].Ch);

            if (y < _height - 1)
                sb.Append(Environment.NewLine);
        }

        return sb.ToString();
    }

    // NEW: Provide a batched view of a row as segments of same color to minimize console calls
    public struct RowSegment
    {
        public int X;
        public string Text;
        public ConsoleColor? Color;
    }

    public IReadOnlyList<RowSegment> GetRowSegments(int row)
    {
        var segments = new List<RowSegment>();
        if (row < 0 || row >= _height || _width == 0) return segments;

        int x = 0;
        while (x < _width)
        {
            // skip trailing spaces at line end? we preserve them to keep alignment
            var col = _cells[row, x].Color;
            var sb = new StringBuilder();
            int start = x;
            while (x < _width && _cells[row, x].Color == col)
            {
                sb.Append(_cells[row, x].Ch);
                x++;
            }

            segments.Add(new RowSegment { X = start, Text = sb.ToString(), Color = col });
        }

        return segments;
    }

}
