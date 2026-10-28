using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>Asks the systems to roll the elements (dice...). Waits ElementsRolled.</summary>
    internal sealed class RollingElementsState : TransitionOnEventState<ElementsRolled>
    {
        public RollingElementsState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new RollElementsRequested());
        }
    }
}
