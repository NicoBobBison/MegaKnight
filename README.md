# MegaKnight
 MegaKnight is a chess engine that plays at the level of an intermediate player. Written in C#, it features both an easy-to-use GUI build and a UCI compatible console build.

## Features
- **User interface**: a simple GUI, with hints for legal moves
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
4. Run the DLL file.

   ```sh
   dotnet MegaKnight.dll
   ```
