public static class InitConsole
{
    private const int DefaultWidth = 160;
    private const int DefaultHeight = 50;

    public static void Apply()
    {
        try
        {
            int targetWidth = Math.Min(DefaultWidth, Console.LargestWindowWidth);
            int targetHeight = Math.Min(DefaultHeight, Console.LargestWindowHeight);

            if (targetWidth < 80 || targetHeight < 25)
                return;

            Console.SetWindowSize(targetWidth, targetHeight);
            Console.SetBufferSize(targetWidth, targetHeight);
        }
        catch
        {
            // Ignore when the host terminal does not allow resizing.
        }
    }
}
