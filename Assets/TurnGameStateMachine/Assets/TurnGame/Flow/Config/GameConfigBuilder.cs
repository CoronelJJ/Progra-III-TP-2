namespace TurnGame.Flow.Config
{
    /// <summary>Convenient way to build a <see cref="GameConfig"/> (specially in tests).</summary>
    public sealed class GameConfigBuilder
    {
        private GameMode _mode = GameMode.SinglePlayer;

        public GameConfigBuilder WithMode(GameMode mode)
        {
            _mode = mode;
            return this;
        }

        public GameConfig Build()
        {
            return new GameConfig(_mode);
        }
    }
}
