using System;
using TurnGame.Core;

namespace TurnGame.Flow.Common
{
    /// <summary>
    /// Convenience base for states. Depends only on <see cref="IStateChanger"/> and <see cref="IMessageBroker"/>.
    /// Pattern for every Enter: subscribe FIRST, publish the request AFTER, so a synchronous
    /// answer is never lost. Every Exit unsubscribes what Enter subscribed.
    /// </summary>
    public abstract class StateBase : IState
    {
        protected IStateChanger Changer { get; }
        protected IMessageBroker Broker { get; }

        protected StateBase(IStateChanger changer, IMessageBroker broker)
        {
            if (changer == null) throw new ArgumentNullException(nameof(changer));
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            Changer = changer;
            Broker = broker;
        }

        public abstract void Enter();

        public virtual void Update(float deltaTime) { }

        public abstract void Exit();

        /// <summary>Requests a direct transition to the next state (applied at a safe point of the tick).</summary>
        protected void GoTo(IState next)
        {
            Changer.ChangeState(this, next);
        }
    }
}
