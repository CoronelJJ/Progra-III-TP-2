using System;

namespace TurnGame.Flow.Config
{
    /// <summary>
    /// Shared context between states that never talk to each other directly.
    /// GameSetup writes the config; InGame reads it (and clears it on exit).
    /// </summary>
    public sealed class GameSession
    {
        public GameConfig Config { get; private set; }

        public bool HasConfig
        {
            get { return Config != null; }
        }

        public void SetConfig(GameConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Config = config;
        }

        public void Clear()
        {
            Config = null;
        }
    }
}
