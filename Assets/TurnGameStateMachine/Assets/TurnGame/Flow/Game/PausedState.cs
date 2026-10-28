using TurnGame.Core;
using TurnGame.Flow.Common;
using TurnGame.Flow.Events;

namespace TurnGame.Flow.Game
{
    /// <summary>
    /// Pause. Entered/left by GameMachine through suspension (the paused state keeps its
    /// subscriptions and is neither Exit-ed nor re-Enter-ed). Its only own transition:
    /// OptionsRequested -> InGameOptions. Resume and quit-to-menu are handled elsewhere.
    /// </summary>
    internal sealed class PausedState : TransitionOnEventState<OptionsRequested>
    {
        public PausedState(IStateChanger changer, IMessageBroker broker) : base(changer, broker) { }
    }
}
