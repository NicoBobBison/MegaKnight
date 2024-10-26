using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaKnight.Core;
using System.CommandLine;
using System.Threading;

namespace MegaKnight.GUI
{
    /// <summary>
    /// This class is run during console entry and creates a terminal window to interact with rather than GUI. UCI compatible.
    /// </summary>
    internal class ConsoleEntry
    {
        BotCore _core;
        CancellationTokenSource cancelTokenSource;
        public ConsoleEntry()
        {
            _core = new BotCore();
        }
        public void Run()
        {
            Console.WriteLine("MegaKnight by NicoBobBison");

            RootCommand rootCommand = SetupRootCommand();
            while (true)
            {
                string input = Console.ReadLine();
                if (input.Trim().ToLower() == "quit") return;

                string[] args = input.Split(' ');
                rootCommand.InvokeAsync(args);
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

            Command uciNewGameCommand = new Command("ucinewgame", "Starts a new game from the next search.");
            uciNewGameCommand.SetHandler(() =>
            {
                _core.StartNewGame();
            });

            Command positionCommand = new Command("position", "Sets a position.");
            Option<bool> startPosOption = new Option<bool>("startpos", "Sets the internal position to the starting position.");
            Option<string[]> fenPositionOption = new Option<string[]>("fen", "Sets the internal position from the FEN string.")
            {
                AllowMultipleArgumentsPerToken = true,
                Arity = new ArgumentArity(6, 6)
            };
            Option<string[]> movesOption = new Option<string[]>("moves", "Moves made from the specified position.")
            {
                AllowMultipleArgumentsPerToken = true
            };
            positionCommand.Add(startPosOption);
            positionCommand.Add(fenPositionOption);
            positionCommand.Add(movesOption);
            positionCommand.SetHandler((startPos, fenString, movesString) =>
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
                    catch
                    {
                        Console.WriteLine("Invalid FEN string.");
                        return;
                    }
                }
                // Console.WriteLine(_core.CurrentPosition.ToString());
                foreach (string s in movesString)
                {
                    Move m = Move.ParseStringToMove(s, _core.CurrentPosition);
                    _core.MakeMoveOnCurrentPosition(m);
                    // Console.WriteLine(_core.CurrentPosition.ToString());
                    // Console.WriteLine(m.DetailedToString(_core.CurrentPosition));
                }
            },
            startPosOption, fenPositionOption, movesOption);

            Command goCommand = new Command("go", "Starts a search.");
            Option<float> wTimeOption = new Option<float>(name: "wtime", description: "Remaining time for white to move (in ms).", getDefaultValue: () => 1000 * 60);
            Option<float> bTimeOption = new Option<float>(name: "btime", description: "Remaining time for black to move (in ms).", getDefaultValue: () => 1000 * 60);
            Option<float> wIncrementOption = new Option<float>(name: "winc", description: "White time increment per move (in ms).", getDefaultValue: () => 1000);
            Option<float> bIncrementOption = new Option<float>(name: "binc", description: "Black time increment per move (in ms).", getDefaultValue: () => 1000);
            Option<bool> infiniteOption = new Option<bool>(name: "infinite", description: "Search until told to stop with the \"stop\" command.");
            goCommand.Add(wTimeOption);
            goCommand.Add(bTimeOption);
            goCommand.Add(wIncrementOption);
            goCommand.Add(bIncrementOption);
            goCommand.Add(infiniteOption);
            goCommand.SetHandler(async (context) =>
            {
                float wTime = context.ParseResult.GetValueForOption(wTimeOption);
                float bTime = context.ParseResult.GetValueForOption(bTimeOption);
                float wInc = context.ParseResult.GetValueForOption(wIncrementOption);
                float bInc = context.ParseResult.GetValueForOption(bIncrementOption);
                bool infinite = context.ParseResult.GetValueForOption(infiniteOption);
                cancelTokenSource = new CancellationTokenSource();

                if (infinite)
                {
                    _core.SetEngineTimeRules(float.MaxValue, float.MaxValue, 0, 0);
                }
                else
                {
                    _core.SetEngineTimeRules(wTime, bTime, wInc, bInc);
                }
                Move bestMove = await _core.GetBestMoveAsync(cancelTokenSource.Token);
                Console.WriteLine("bestmove " + bestMove.ToString());
            });

            Command stopCommand = new Command("stop", "Stops the current search.");
            stopCommand.SetHandler(() =>
            {
                cancelTokenSource?.Cancel();
            });

            Command isReadyCommand = new Command("isready", "Checks if the engine is ready to process commands.");
            isReadyCommand.SetHandler(async () =>
            {
                await Task.Run(async () =>
                {
                    while (!_core.IsReady)
                    {
                        await Task.Delay(5);
                    }
                });
                Console.WriteLine("readyok");
            });

            RootCommand rootCommand = new RootCommand()
            {
                uciCommand,
                uciNewGameCommand,
                positionCommand,
                goCommand,
                stopCommand,
                isReadyCommand
            };
            return rootCommand;
        }
    }
}
