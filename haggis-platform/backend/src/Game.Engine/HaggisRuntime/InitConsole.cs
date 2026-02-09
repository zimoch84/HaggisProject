public static class InitConsole
{
    public static void Apply()
    {
        try
        {
            int width = 170;
            int height = 55;

            // ZAWSZE najpierw bufor
            Console.SetBufferSize(width, height);

            // potem okno
            Console.SetWindowSize(
                Math.Min(width, Console.LargestWindowWidth),
                Math.Min(height, Console.LargestWindowHeight)
            );
        }
        catch
        {
            // ignorujemy na małych ekranach
        }
    }
}
