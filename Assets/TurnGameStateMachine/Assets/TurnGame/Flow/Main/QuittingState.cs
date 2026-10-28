using System;
using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Services;

namespace TurnGame.Flow.Main
{
    /// <summary>Terminal state. Delegates to IApplicationQuitter (never calls Application.Quit directly).</summary>
    public sealed class QuittingState : StateBase
    {
        private readonly IApplicationQuitter _quitter;

        public QuittingState(IStateChanger changer, IMessageBroker broker, IApplicationQuitter quitter) : base(changer, broker)
        {
            if (quitter == null) throw new ArgumentNullException(nameof(quitter));
            _quitter = quitter;
        }

        public override void Enter()
        {
            _quitter.Quit();
        }

        public override void Exit() { }
    }
}
