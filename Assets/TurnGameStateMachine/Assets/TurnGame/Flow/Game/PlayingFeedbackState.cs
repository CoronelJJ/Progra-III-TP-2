using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>Feedback of the resolved play. Waits FeedbackFinished and goes back to CheckingBoard.</summary>
    internal sealed class PlayingFeedbackState : TransitionOnEventState<FeedbackFinished>
    {
        public PlayingFeedbackState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        protected override void OnEntered()
        {
            Broker.Publish(new PlayFeedbackRequested());
        }
    }
}
