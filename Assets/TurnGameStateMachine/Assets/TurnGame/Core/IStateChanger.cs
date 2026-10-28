namespace TurnGame.Core
{
    /// <summary>
    /// Minimal surface a state needs to request a direct transition.
    /// States receive this instead of the concrete machine, so they can be tested with a fake.
    /// </summary>
    public interface IStateChanger
    {
        /// <summary>
        /// Requests a transition from <paramref name="source"/> to <paramref name="target"/>.
        /// The request is queued and applied at a safe point of the tick (never re-entrantly).
        /// <paramref name="source"/> identifies who is asking, so requests from states that are
        /// not the active one can be ignored (or deferred, if the source is the suspended state).
        /// </summary>
        void ChangeState(IState source, IState target);
    }
}
