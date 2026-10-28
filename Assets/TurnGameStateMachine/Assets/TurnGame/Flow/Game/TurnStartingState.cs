using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>Start of a turn (may host an intro animation). Waits TurnIntroFinished.</summary>
    internal sealed class TurnStartingState : TransitionOnEventState<TurnIntroFinished>
    {
        public TurnStartingState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new TurnStartRequested());
        }
    }
}
