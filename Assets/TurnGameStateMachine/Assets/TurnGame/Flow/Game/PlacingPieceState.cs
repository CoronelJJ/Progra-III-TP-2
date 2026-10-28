using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>The player places the piece. Waits PiecePlaced; PieceMoved is consumed by the preview system, not by this state.</summary>
    internal sealed class PlacingPieceState : TransitionOnEventState<PiecePlaced>
    {
        public PlacingPieceState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new PlacePieceRequested());
        }
    }
}
