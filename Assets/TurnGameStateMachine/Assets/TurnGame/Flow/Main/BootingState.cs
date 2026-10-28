using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Main
{
    /// <summary>Initial state. Asks the systems to boot and waits for BootCompleted.</summary>
    public sealed class BootingState : TransitionOnEventState<BootCompleted>
    {
        public BootingState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new BootRequested());
        }
    }
}
