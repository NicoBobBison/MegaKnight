#if GUI || DEBUG
using var game = new MegaKnight.GUI.GUIEntry();
game.Run();
#elif CONSOLE
using MegaKnight;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        MegaKnight.GUI.ConsoleEntry c = new MegaKnight.GUI.ConsoleEntry();
        c.Run();
    }
}
#endif