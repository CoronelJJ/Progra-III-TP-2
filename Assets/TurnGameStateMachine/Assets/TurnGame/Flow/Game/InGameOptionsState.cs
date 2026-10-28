using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>Options opened from the pause menu. OptionsClosed returns to Paused (not to the suspended state).</summary>
    internal sealed class InGameOptionsState : TransitionOnEventState<OptionsClosed>
    {
        public InGameOptionsState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new OptionsScreenRequested());
        }
    }
}
