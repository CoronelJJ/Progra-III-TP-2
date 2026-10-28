using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Main
{
    /// <summary>Options opened from the main menu. OptionsClosed returns to MainMenu.</summary>
    public sealed class MenuOptionsState : TransitionOnEventState<OptionsClosed>
    {
        public MenuOptionsState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new OptionsScreenRequested());
        }
    }
}
