#if GUI
using var game = new MegaKnight.GUIEntry();
game.Run();
#elif CONSOLE
using MegaKnight;

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleEntry c = new ConsoleEntry();
        c.Run();
    }
}
#endif