using System;
using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Config;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;

namespace TurnGame.Flow.Main
{
    /// <summary>
    /// Owner of the GameMachine sub-machine. It is the ONLY place that creates/reaches it
    /// (GameMachine is internal), and it is only entered from GameSetup.
    /// Returns to MainMenu on GameFinished, or on QuitToMenuRequested while the game is paused.
    /// </summary>
    public sealed class InGameState : StateBase
    {
        private readonly GameSession _session;
        private IState _mainMenu;
        private GameMachine _game;

        public InGameState(IStateChanger changer, IMessageBroker broker, GameSession session) : base(changer, broker)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _session = session;
        }

        public void SetNext(IState mainMenu)
        {
            if (mainMenu == null) throw new ArgumentNullException(nameof(mainMenu));
            _mainMenu = mainMenu;
        }

        public override void Enter()
        {
            if (_mainMenu == null)
                throw new InvalidOperationException(nameof(InGameState) + ": SetNext() was not called.");
            if (!_session.HasConfig)
                throw new InvalidOperationException(nameof(InGameState) + " can only be entered from GameSetup (no GameConfig in session).");

            Broker.Subscribe<GameFinished>(OnGameFinished);
            Broker.Subscribe<QuitToMenuRequested>(OnQuitToMenuRequested);

            _game = new GameMachine(Broker);
            _game.Start();
        }

        public override void Update(float deltaTime)
        {
            if (_game != null) _game.Update(deltaTime);
        }

        public override void Exit()
        {
            Broker.Unsubscribe<GameFinished>(OnGameFinished);
            Broker.Unsubscribe<QuitToMenuRequested>(OnQuitToMenuRequested);

            if (_game != null)
            {
                _game.Stop(); // exits the active AND the suspended game state
                _game = null;
            }
            _session.Clear();
        }

        private void OnGameFinished(GameFinished _)
        {
            GoTo(_mainMenu);
        }

        private void OnQuitToMenuRequested(QuitToMenuRequested _)
        {
            if (_game != null && _game.IsInPauseMenu)
                GoTo(_mainMenu);
        }
    }
}
