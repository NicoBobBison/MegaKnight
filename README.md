# MegaKnight

 MegaKnight is a chess engine that plays at the level of an intermediate player. Written in C#, it features both an easy-to-use GUI build and a UCI compatible console build.

## Features

- **User interface**: a simple GUI built in MonoGame, with hints for legal moves
- **Compatability with other GUIs**: MegaKnight includes a console build compatible with the Universal Chess Interface (UCI), allowing it to be used with other GUIs or benchmarked with programs like [Fastchess](https://github.com/Disservin/fastchess).
- **Move validation**: ensures all moves made are legal
- **Engine play**: analyzes the current board state and plays the best possible move

## Getting Started

### Prerequisites
1. Download and install [the .NET framework](https://dotnet.microsoft.com/en-us/download).

### Installation
1. Clone the repository and navigate into its directory.

   ```sh
   cd MegaKnight
   ```
3. Build the GUI version:

   ```sh
   dotnet build MegaKnight.csproj -c GUI -o [OUTPUT DIRECTORY]
   ```
   
   Alternatively, build the console version:

   ```sh
   dotnet build MegaKnight.csproj -c CONSOLE -o [OUTPUT DIRECTORY]
   ```
   
4. Run the .exe file.
   
   ```sh
   start MegaKnight.exe
   ```
   
   Alternatively, for the console build, you may also run the DLL file.

   ```sh
   dotnet MegaKnight.dll
   ```
   
## Roadmap
MegaKnight is currently in active development. Some upcoming features include:
- [ ] Adding better pruning techniques to increase engine performance
- [ ] Rewriting move generation to be faster
- [ ] Updating the GUI to include more functionality, like undoing moves, restarting the game, and switching sides
- [ ] Implement an opening book

## Acknowledgements
- [Fastchess (for benchmarking and testing)](https://github.com/Disservin/fastchess)
- [Opening book](https://github.com/official-stockfish/books/blob/master/8moves_v3.pgn.zip) (Note: this isn't actually implemented in play yet, this is just the book used for benchmarking on Fastchess)
- [The Chess Programming Wiki](https://www.chessprogramming.org/Main_Page)
   - Notably, [PeSTO's evaluation function](https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function) for piece-square tables and material values
- [An article about bitboard intuition](https://lichess.org/@/likeawizard/blog/review-of-different-board-representations-in-computer-chess/S9eQCAWa)
- [An article on fast move generation](https://peterellisjones.com/posts/generating-legal-chess-moves-efficiently/)
- [Coding Adventure: Chess](https://www.youtube.com/watch?v=U4ogK0MIzqk)
