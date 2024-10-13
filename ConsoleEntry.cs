using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaKnight.Core;
using System.CommandLine;

namespace MegaKnight
{
    /// <summary>
    /// This class is run during console entry and creates a terminal window to interact with rather than GUI. UCI compatible.
    /// </summary>
    internal class ConsoleEntry
    {
        BotCore _core;
        public ConsoleEntry()
        {
            _core = new BotCore();
        }
        public async Task Run()
        {
            RootCommand rootCommand = new RootCommand();
            while (true)
            {
                string[] args = Console.ReadLine().Split(' ');
                await rootCommand.InvokeAsync(args);
            }
/*            Console.WriteLine("MegaKnight by NicoBobBison");
            string args = Console.ReadLine();
            while (args != "exit")
            {
                Process(args);
                args = Console.ReadLine();
            }
*/        }
        void Process(string argsString)
        {
            string[] args = argsString.Split(' ');
            if (args[0] == "uci")
            {
                Console.WriteLine("id name MegaKnight");
                Console.WriteLine("id author NicoBobBison");
                // TODO: Add configuration options
                Console.WriteLine("uciok");
            }
            else if (args[0] == "position")
            {
                if(args.Length >= 2)
                {
                    if (args[1] == "startpos")
                    {
                        _core.SetPositionToStartPosition();
                    }
                    else if (args[1] == "fen")
                    {
                        if(args.Length >= 3)
                        {
                            string str = "";
                            for(int i = 2; i < args.Length; i++)
                            {
                                str += args[i];
                                if (i + 1 < args.Length) str += " ";
                            }
                            Console.WriteLine("Setting '" + str + "' as position");
                            _core.SetPositionFromFEN(str);
                        }
                    }
                }
            }
            else if (args[0] == "go")
            {

            }
            else
            {
                Console.WriteLine("Invalid command");
            }
        }
    }
}
