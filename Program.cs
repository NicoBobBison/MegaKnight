#if GUI || DEBUG
using var game = new MegaKnight.GUIEntry();
game.Run();
#elif CONSOLE
using MegaKnight;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleEntry c = new ConsoleEntry();
        c.Run();
    }
}
#endif