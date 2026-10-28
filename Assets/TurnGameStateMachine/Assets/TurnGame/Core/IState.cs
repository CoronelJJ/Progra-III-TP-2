namespace TurnGame.Core
{
    /// <summary>
    /// Contract every state implements. Machines and neighbouring states only ever
    /// depend on this interface, never on concrete state classes.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when the state becomes active.</summary>
        void Enter();

        /// <summary>Called every tick while the state is the active one.</summary>
        /// <param name="deltaTime">Seconds since the previous tick (injected, never read from Unity).</param>
        void Update(float deltaTime);

        /// <summary>Called once when the state stops being active for good.</summary>
        void Exit();
    }
}
