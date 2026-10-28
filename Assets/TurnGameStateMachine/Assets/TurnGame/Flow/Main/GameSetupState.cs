using System;
using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Config;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Main
{
    /// <summary>
    /// Mode selection + scene setup.
    ///   1. Waits GameModeSelected, builds the immutable GameConfig, stores it in the session
    ///      and publishes SetupRequested(config).
    ///   2. Waits SetupFinished (systems may answer synchronously) and goes to InGame.
    /// SetupCancelled returns to MainMenu at any moment.
    /// </summary>
    public sealed class GameSetupState : StateBase
    {
        private readonly GameSession _session;
        private IState _inGame;
        private IState _mainMenu;
        private bool _setupRequested;

        public GameSetupState(IStateChanger changer, IMessageBroker broker, GameSession session) : base(changer, broker)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _session = session;
        }

        public void SetNext(IState inGame, IState mainMenu)
        {
            if (inGame == null) throw new ArgumentNullException(nameof(inGame));
            if (mainMenu == null) throw new ArgumentNullException(nameof(mainMenu));
            _inGame = inGame;
            _mainMenu = mainMenu;
        }

        public override void Enter()
        {
            if (_inGame == null)
                throw new InvalidOperationException(nameof(GameSetupState) + ": SetNext() was not called.");

            _setupRequested = false;
            Broker.Subscribe<GameModeSelected>(OnGameModeSelected);
            Broker.Subscribe<SetupFinished>(OnSetupFinished);
            Broker.Subscribe<SetupCancelled>(OnSetupCancelled);
            Broker.Publish(new GameModeSelectionRequested());
        }

        public override void Exit()
        {
            Broker.Unsubscribe<GameModeSelected>(OnGameModeSelected);
            Broker.Unsubscribe<SetupFinished>(OnSetupFinished);
            Broker.Unsubscribe<SetupCancelled>(OnSetupCancelled);
        }

        private void OnGameModeSelected(GameModeSelected selected)
        {
            if (_setupRequested) return; // only the first selection counts

            _setupRequested = true; // set BEFORE publishing: a synchronous SetupFinished must be accepted
            var config = new GameConfigBuilder().WithMode(selected.Mode).Build();
            _session.SetConfig(config);
            Broker.Publish(new SetupRequested(config));
        }

        private void OnSetupFinished(SetupFinished _)
        {
            if (!_setupRequested) return;
            GoTo(_inGame);
        }

        private void OnSetupCancelled(SetupCancelled _)
        {
            _session.Clear();
            GoTo(_mainMenu);
        }
    }
}
