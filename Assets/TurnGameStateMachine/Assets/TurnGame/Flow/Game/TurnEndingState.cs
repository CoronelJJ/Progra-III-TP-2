using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>End of turn. Waits NextTurnReady; turns are infinite (game over is a global transition).</summary>
    internal sealed class TurnEndingState : TransitionOnEventState<NextTurnReady>
    {
        public TurnEndingState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new TurnEndRequested());
        }
    }
}
