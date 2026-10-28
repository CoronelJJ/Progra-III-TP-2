using TurnGame.Flow.Config;

namespace TurnGame.Flow.Events
{
    // Convention: facts are in the past tense (BootCompleted), requests end in "Requested".
    // "...Requested" events published BY a state ask an external system (UI, loader...) to do something.
    // "...Requested" events published TO the machine express an intention (PlayRequested, QuitRequested).

    // ---- Booting
    public readonly struct BootRequested { }          // state -> systems: start loading
    public readonly struct BootCompleted { }          // systems -> state

    // ---- MainMenu
    public readonly struct MainMenuScreenRequested { } // state -> UI: show the menu
    public readonly struct PlayRequested { }           // UI -> state
    public readonly struct OptionsRequested { }        // UI -> state (MainMenu and Paused share it; only the active state listens)
    public readonly struct QuitRequested { }           // UI -> state

    // ---- Options (MenuOptions and InGameOptions)
    public readonly struct OptionsScreenRequested { }  // state -> UI: show the options screen
    public readonly struct OptionsClosed { }           // UI -> state

    // ---- GameSetup
    public readonly struct GameModeSelectionRequested { } // state -> UI: show the mode selector
    public readonly struct GameModeSelected                // UI -> state
    {
        public readonly GameMode Mode;
        public GameModeSelected(GameMode mode) { Mode = mode; }
    }
    public readonly struct SetupRequested                  // state -> scene/systems: build the game with this config
    {
        public readonly GameConfig Config;
        public SetupRequested(GameConfig config) { Config = config; }
    }
    public readonly struct SetupFinished { }               // systems -> state (may be published synchronously)
    public readonly struct SetupCancelled { }              // UI -> state

    // ---- InGame
    public readonly struct QuitToMenuRequested { }         // pause UI -> InGame (only honoured while Paused)
    public readonly struct GameFinished { }                // GameOverScreen -> InGame
}
