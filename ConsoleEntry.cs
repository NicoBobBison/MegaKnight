using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaKnight.Core;

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
        public void Run()
        {
            Console.WriteLine("MegaKnight by NicoBobBison");
            string[] args = Console.ReadLine().Split(' ');
            while (args[0] != "exit")
            {
                Process(args);
                args = Console.ReadLine().Split(' ');
            }
        }
        void Process(string[] args)
        {
            if (args[0] == "uci")
            {
                Console.WriteLine("id name MegaKnight");
                Console.WriteLine("id author NicoBobBison");
                // TODO: Add configuration options
                Console.WriteLine("uciok");
            }
            
        }
    }
}
