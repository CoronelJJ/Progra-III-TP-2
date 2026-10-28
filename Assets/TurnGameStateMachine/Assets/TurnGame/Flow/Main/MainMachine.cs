using System;
using TurnGame.Core;
using TurnGame.Flow.Config;
using TurnGame.Flow.Services;

namespace TurnGame.Flow.Main
{
    /// <summary>
    /// Top-level machine: Booting, MainMenu, MenuOptions, GameSetup, InGame, Quitting.
    /// Builds all states, wires the (cyclic) transitions and drives them.
    /// </summary>
    public sealed class MainMachine
    {
        private readonly StateMachine _machine = new StateMachine();
        private readonly BootingState _booting;

        public GameSession Session { get; }

        /// <summary>Active state. Compare by type in tests: Assert.IsInstanceOf&lt;MainMenuState&gt;(machine.Current).</summary>
        public IState Current
        {
            get { return _machine.Current; }
        }

        public MainMachine(IMessageBroker broker, IApplicationQuitter quitter) : this(broker, quitter, new GameSession()) { }

        public MainMachine(IMessageBroker broker, IApplicationQuitter quitter, GameSession session)
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            if (quitter == null) throw new ArgumentNullException(nameof(quitter));
            if (session == null) throw new ArgumentNullException(nameof(session));
            Session = session;

            _booting = new BootingState(_machine, broker);
            var mainMenu = new MainMenuState(_machine, broker);
            var menuOptions = new MenuOptionsState(_machine, broker);
            var gameSetup = new GameSetupState(_machine, broker, session);
            var inGame = new InGameState(_machine, broker, session);
            var quitting = new QuittingState(_machine, broker, quitter);

            _booting.SetNext(mainMenu);
            mainMenu.SetNext(gameSetup, menuOptions, quitting);
            menuOptions.SetNext(mainMenu);
            gameSetup.SetNext(inGame, mainMenu);
            inGame.SetNext(mainMenu);
        }

        public void Start() { _machine.Start(_booting); }

        public void Update(float deltaTime) { _machine.Update(deltaTime); }

        /// <summary>Exits every active state (including the game sub-machine) and unsubscribes everything.</summary>
        public void Stop() { _machine.Stop(); }
    }
}
