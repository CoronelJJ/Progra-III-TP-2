using System;
using TurnGame.Core;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>
    /// Turn-based game sub-machine. INTERNAL: only InGameState can create and reach it.
    ///
    /// Flow:   TurnStarting -> RollingElements -> BuildingPiece -> PlacingPiece -> UpdatingBoard
    ///         -> CheckingBoard -> (ResolvingPlay -> PlayingFeedback -> CheckingBoard)* -> TurnEnding -> TurnStarting
    ///
    /// Global transitions, handled here (not by the states):
    ///   PauseRequested   -> suspends the active state (no Exit) and enters Paused      [not from Paused/InGameOptions/GameOverScreen]
    ///   ResumeRequested  -> leaves Paused and restores the suspended state (no Enter)  [only from Paused]
    ///   GameOverReached  -> exits active + suspended and enters GameOverScreen         [from anywhere but GameOverScreen]
    /// All of them are queued and applied at a safe point of the tick; GameOverReached drops whatever was pending.
    /// </summary>
    internal sealed class GameMachine
    {
        private readonly IMessageBroker _broker;
        private readonly StateMachine _machine = new StateMachine();

        private readonly TurnStartingState _turnStarting;
        private readonly PausedState _paused;
        private readonly InGameOptionsState _inGameOptions;
        private readonly GameOverScreenState _gameOverScreen;

        private bool _started;

        public GameMachine(IMessageBroker broker)
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            _broker = broker;

            _turnStarting = new TurnStartingState(_machine, broker);
            var rollingElements = new RollingElementsState(_machine, broker);
            var buildingPiece = new BuildingPieceState(_machine, broker);
            var placingPiece = new PlacingPieceState(_machine, broker);
            var updatingBoard = new UpdatingBoardState(_machine, broker);
            var checkingBoard = new CheckingBoardState(_machine, broker);
            var resolvingPlay = new ResolvingPlayState(_machine, broker);
            var playingFeedback = new PlayingFeedbackState(_machine, broker);
            var turnEnding = new TurnEndingState(_machine, broker);
            _paused = new PausedState(_machine, broker);
            _inGameOptions = new InGameOptionsState(_machine, broker);
            _gameOverScreen = new GameOverScreenState(_machine, broker);

            _turnStarting.SetNext(rollingElements);
            rollingElements.SetNext(buildingPiece);
            buildingPiece.SetNext(placingPiece);
            placingPiece.SetNext(updatingBoard);
            updatingBoard.SetNext(checkingBoard);
            checkingBoard.SetNext(resolvingPlay, turnEnding);
            resolvingPlay.SetNext(playingFeedback);
            playingFeedback.SetNext(checkingBoard);
            turnEnding.SetNext(_turnStarting);
            _paused.SetNext(_inGameOptions);
            _inGameOptions.SetNext(_paused);
        }

        public IState Current
        {
            get { return _machine.Current; }
        }

        /// <summary>The state parked while Paused/InGameOptions are active, or null.</summary>
        public IState Suspended
        {
            get { return _machine.Suspended; }
        }

        public bool IsInPauseMenu
        {
            get { return ReferenceEquals(_machine.Current, _paused); }
        }

        public void Start()
        {
            if (_started) throw new InvalidOperationException("GameMachine already started.");
            _started = true;

            _broker.Subscribe<PauseRequested>(OnPauseRequested);
            _broker.Subscribe<ResumeRequested>(OnResumeRequested);
            _broker.Subscribe<GameOverReached>(OnGameOverReached);

            _machine.Start(_turnStarting);
        }

        public void Update(float deltaTime)
        {
            _machine.Update(deltaTime);
        }

        /// <summary>Exits the active and the suspended state and unsubscribes the global handlers.</summary>
        public void Stop()
        {
            if (!_started) return;
            _started = false;

            _broker.Unsubscribe<PauseRequested>(OnPauseRequested);
            _broker.Unsubscribe<ResumeRequested>(OnResumeRequested);
            _broker.Unsubscribe<GameOverReached>(OnGameOverReached);

            _machine.Stop();
        }

        // ------------------------------------------------------------ global handlers

        private void OnPauseRequested(PauseRequested _)
        {
            _machine.Enqueue(() =>
            {
                if (!IsPausable(_machine.Current)) return;
                _machine.Suspend(_paused);
                _broker.Publish(new GamePaused());
            });
        }

        private void OnResumeRequested(ResumeRequested _)
        {
            _machine.Enqueue(() =>
            {
                if (!ReferenceEquals(_machine.Current, _paused)) return;
                _machine.Resume();
                _broker.Publish(new GameResumed());
            });
        }

        private void OnGameOverReached(GameOverReached _)
        {
            if (ReferenceEquals(_machine.Current, _gameOverScreen)) return;

            _machine.EnqueueOverriding(() =>
            {
                if (ReferenceEquals(_machine.Current, _gameOverScreen)) return;
                _machine.ForceChange(_gameOverScreen);
            });
        }

        private bool IsPausable(IState state)
        {
            return state != null
                && !ReferenceEquals(state, _paused)
                && !ReferenceEquals(state, _inGameOptions)
                && !ReferenceEquals(state, _gameOverScreen);
        }
    }
}
