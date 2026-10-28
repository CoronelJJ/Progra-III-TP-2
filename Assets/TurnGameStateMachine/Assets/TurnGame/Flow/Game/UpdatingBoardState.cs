using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>Board update after the placement. Waits BoardUpdated.</summary>
    internal sealed class UpdatingBoardState : TransitionOnEventState<BoardUpdated>
    {
        public UpdatingBoardState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new UpdateBoardRequested());
        }
    }
}
