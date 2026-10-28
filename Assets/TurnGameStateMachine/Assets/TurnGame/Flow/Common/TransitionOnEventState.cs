using System;
using TurnGame.Core;

namespace TurnGame.Flow.Common
{
    /// <summary>
    /// A state that waits for ONE event and then moves to its single next state.
    /// Subclasses may publish a request in <see cref="OnEntered"/> (runs after subscribing).
    /// The next state is injected after construction with <see cref="SetNext"/> because the
    /// state graph has cycles (TurnEnding -> TurnStarting, PlayingFeedback -> CheckingBoard...).
    /// </summary>
    public abstract class TransitionOnEventState<TEvent> : StateBase
    {
        private IState _next;

        protected TransitionOnEventState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }

        public void SetNext(IState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            _next = next;
        }

        public sealed override void Enter()
        {
            if (_next == null)
                throw new InvalidOperationException(GetType().Name + ": SetNext() was not called.");

            Broker.Subscribe<TEvent>(OnTrigger);
            OnEntered();
        }

        public sealed override void Exit()
        {
            Broker.Unsubscribe<TEvent>(OnTrigger);
            OnExited();
        }

        /// <summary>Runs after subscribing. Publish the request to external systems here.</summary>
        protected virtual void OnEntered() { }

        protected virtual void OnExited() { }

        private void OnTrigger(TEvent message)
        {
            GoTo(_next);
        }
    }
}
