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
            Console.WriteLine("MegaKnight by NicoBobBison");

            RootCommand rootCommand = SetupRootCommand();
            while (true)
            {
                string input = Console.ReadLine();
                if (input.Trim().ToLower() == "quit") return;

                string[] args = input.Split(' ');
                await rootCommand.InvokeAsync(args);
            }
        }
        public RootCommand SetupRootCommand()
        {
            Command uciCommand = new Command("uci", "Provides engine name and author, and gives a list of options to set via setoptions. Prints uciok afterwards.");
            uciCommand.SetHandler(() =>
            {
                Console.WriteLine("id name MegaKnight");
                Console.WriteLine("id author NicoBobBison");
                // TODO: Add configuration options
                Console.WriteLine("uciok");
            });

            Command positionCommand = new Command("position", "Sets a position.");
            Option<bool> startPosOption = new Option<bool>("startpos", "Sets the internal position to the starting position.");
            Option<string[]> fenPositionOption = new Option<string[]>("fen", "Sets the internal position from the FEN string.")
            {
                AllowMultipleArgumentsPerToken = true
            };
            positionCommand.Add(startPosOption);
            positionCommand.Add(fenPositionOption);
            positionCommand.SetHandler((startPos, fenString) =>
            {
                if (startPos)
                {
                    _core.SetPositionToStartPosition();
                }
                else
                {
                    try
                    {
                        string str = "";
                        for (int i = 0; i < fenString.Length; i++)
                        {
                            str += fenString[i];
                            if (i + 1 < fenString.Length) str += " ";
                        }
                        _core.SetPositionFromFEN(str);
                    }
                    catch { } // We don't do anything if an error is thrown
                    
                }
            },
            startPosOption, fenPositionOption);

            Command goCommand = new Command("go", "Starts a search.");
            Option<float> wTimeOption = new Option<float>("wtime", "Remaining time for white to move.");
            Option<float> bTimeOption = new Option<float>("btime", "Remaining time for black to move.");
            Option<float> wIncrementOption = new Option<float>("winc", "White time increment per move.");
            Option<float> bIncrementOption = new Option<float>("binc", "Black time increment per move.");
            goCommand.SetHandler((wTime, bTime, wInc, bInc) =>
            {
             
            },
            wTimeOption, bTimeOption, wIncrementOption, bIncrementOption);

            RootCommand rootCommand = new RootCommand()
            {
                uciCommand,
                positionCommand,
            };
            return rootCommand;
        }
    }
}
