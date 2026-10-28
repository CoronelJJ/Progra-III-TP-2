using System.Runtime.CompilerServices;

// GameMachine (and its states) are internal on purpose: the only way to reach the game
// sub-machine is through InGameState. The test assembly is the only other one allowed in.
[assembly: InternalsVisibleTo("TurnGame.Tests.EditMode")]
