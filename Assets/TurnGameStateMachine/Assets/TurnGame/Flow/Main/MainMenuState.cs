using System;
using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Main
{
    /// <summary>Main menu. Three possible destinations, decided by which event arrives.</summary>
    public sealed class MainMenuState : StateBase
    {
        private IState _gameSetup;
        private IState _menuOptions;
        private IState _quitting;

        public MainMenuState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        public void SetNext(IState gameSetup, IState menuOptions, IState quitting)
        {
            if (gameSetup == null) throw new ArgumentNullException(nameof(gameSetup));
            if (menuOptions == null) throw new ArgumentNullException(nameof(menuOptions));
            if (quitting == null) throw new ArgumentNullException(nameof(quitting));
            _gameSetup = gameSetup;
            _menuOptions = menuOptions;
            _quitting = quitting;
        }

        public override void Enter()
        {
            if (_gameSetup == null)
                throw new InvalidOperationException(nameof(MainMenuState) + ": SetNext() was not called.");

            Broker.Subscribe<PlayRequested>(OnPlayRequested);
            Broker.Subscribe<OptionsRequested>(OnOptionsRequested);
            Broker.Subscribe<QuitRequested>(OnQuitRequested);
            Broker.Publish(new MainMenuScreenRequested());
        }

        public override void Exit()
        {
            Broker.Unsubscribe<PlayRequested>(OnPlayRequested);
            Broker.Unsubscribe<OptionsRequested>(OnOptionsRequested);
            Broker.Unsubscribe<QuitRequested>(OnQuitRequested);
        }

        private void OnPlayRequested(PlayRequested _) { GoTo(_gameSetup); }
        private void OnOptionsRequested(OptionsRequested _) { GoTo(_menuOptions); }
        private void OnQuitRequested(QuitRequested _) { GoTo(_quitting); }
    }
}
