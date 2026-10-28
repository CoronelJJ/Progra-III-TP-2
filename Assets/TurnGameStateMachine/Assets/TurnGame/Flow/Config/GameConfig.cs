namespace TurnGame.Flow.Config
{
    /// <summary>
    /// Immutable configuration of a match. Parameters are not final yet: add them here
    /// (readonly, set through the constructor) and expose them in <see cref="GameConfigBuilder"/>.
    /// </summary>
    public sealed class GameConfig
    {
        public GameMode Mode { get; }

        public GameConfig(GameMode mode)
        {
            Mode = mode;
        }

        public static GameConfig Default
        {
            get { return new GameConfigBuilder().Build(); }
        }
    }
}
