using System;
using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>
    /// Asks the board to look for plays. Two destinations: a play was detected (ResolvingPlay)
    /// or not (TurnEnding). Stacked dice can chain plays; the machine does not care, it just
    /// receives PlayDetected / NoPlayDetected every time it re-enters this state.
    /// </summary>
    internal sealed class CheckingBoardState : StateBase
    {
        private IState _resolvingPlay;
        private IState _turnEnding;

        public CheckingBoardState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        public void SetNext(IState resolvingPlay, IState turnEnding)
        {
            if (resolvingPlay == null) throw new ArgumentNullException(nameof(resolvingPlay));
            if (turnEnding == null) throw new ArgumentNullException(nameof(turnEnding));
            _resolvingPlay = resolvingPlay;
            _turnEnding = turnEnding;
        }

        public override void Enter()
        {
            if (_resolvingPlay == null)
                throw new InvalidOperationException(nameof(CheckingBoardState) + ": SetNext() was not called.");

            Broker.Subscribe<PlayDetected>(OnPlayDetected);
            Broker.Subscribe<NoPlayDetected>(OnNoPlayDetected);
            Broker.Publish(new CheckBoardRequested());
        }

        public override void Exit()
        {
            Broker.Unsubscribe<PlayDetected>(OnPlayDetected);
            Broker.Unsubscribe<NoPlayDetected>(OnNoPlayDetected);
        }

        private void OnPlayDetected(PlayDetected _) { GoTo(_resolvingPlay); }
        private void OnNoPlayDetected(NoPlayDetected _) { GoTo(_turnEnding); }
    }
}
