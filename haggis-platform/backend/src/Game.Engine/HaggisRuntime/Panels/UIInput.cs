using System;
using Haggis.Model;

public class UIInput : PanelRegionBase
{
    private string _prompt;

    public UIInput(string header, int x, int y, int width, int height) : base(header, x, y, width, height)
    {
        IsVisible = true;
    }



    public override void ApplyTextBuffer(HaggisGameState state)
    {
        // Domyślny tekst (można rozbudować o listę akcji itp.)
        TextBuffer.Clear();
        int maxIndex = state.Actions.Count - 1;
        _prompt = $"Wprowadź numer [0..{maxIndex}]: ";
        TextBuffer.Write(_prompt);
        
        
    }

    public int ReadHumanInput(HaggisGameState state)
    {
        // Bezpieczny fallback
        if (state == null || state.Actions == null || state.Actions.Count == 0)
            return 0;

        int maxIndex = state.Actions.Count - 1;

        while (true)
        {
            int cursorX = this.X + 1 + _prompt.Length;
            int cursorY = this.Y + 1;
            SafeSetCursor(cursorX, cursorY);
            Console.CursorVisible = true;
            string? input = Console.ReadLine();
            Console.CursorVisible = false;

            if (string.IsNullOrWhiteSpace(input))
            {
                TextBuffer.WriteLine("Nie podano żadnego wejścia. Spróbuj ponownie.");
                this.DrawState(state);
                continue;
            }

            if (int.TryParse(input.Trim(), out int index))
            {
                if (index >= 0 && index <= maxIndex)
                    return index;

                TextBuffer.WriteLine($"Liczba spoza zakresu. Wybierz wartość od 0 do {maxIndex}.");
                this.DrawState(state);
            }
            else
            {
                TextBuffer.WriteLine("Niepoprawny format. Wprowadź liczbę całkowitą.");
                this.DrawState(state);
            }
        }
    }
     private void SafeSetCursor(int x, int y)
        {
            int maxY = Console.BufferHeight - 1;
            int maxX = Console.BufferWidth - 1;

            x = Math.Max(0, Math.Min(x, maxX));
            y = Math.Max(0, Math.Min(y, maxY));

            Console.SetCursorPosition(x, y);
        }
}
