using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>
    /// Results screen. Entered through the global GameOverReached transition. Waits for
    /// GameOverDismissed and then publishes GameFinished (InGame handles it and returns to MainMenu).
    /// Not pausable and ignores a second GameOverReached.
    /// </summary>
    internal sealed class GameOverScreenState : StateBase
    {
        private bool _finished;

        public GameOverScreenState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        public override void Enter()
        {
            _finished = false;
            Broker.Subscribe<GameOverDismissed>(OnGameOverDismissed);
            Broker.Publish(new GameOverScreenRequested());
        }

        public override void Exit()
        {
            Broker.Unsubscribe<GameOverDismissed>(OnGameOverDismissed);
        }

        private void OnGameOverDismissed(GameOverDismissed _)
        {
            if (_finished) return;
            _finished = true;
            Broker.Publish(new GameFinished());
        }
    }
}
