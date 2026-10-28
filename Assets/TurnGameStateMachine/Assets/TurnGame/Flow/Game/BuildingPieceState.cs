using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>The player builds the piece (complex input, no cancel). Waits PieceBuilt.</summary>
    internal sealed class BuildingPieceState : TransitionOnEventState<PieceBuilt>
    {
        public BuildingPieceState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new BuildPieceRequested());
        }
    }
}
