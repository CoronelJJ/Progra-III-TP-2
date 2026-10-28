using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>Resolution of a detected play. Waits PlayResolved.</summary>
    internal sealed class ResolvingPlayState : TransitionOnEventState<PlayResolved>
    {
        public ResolvingPlayState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new ResolvePlayRequested());
        }
    }
}
