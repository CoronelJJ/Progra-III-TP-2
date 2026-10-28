using System;
using System.Collections.Generic;

namespace TurnGame.Core
{
    /// <summary>
    /// Engine-free state machine. It only sequences states: it never computes game logic.
    ///
    /// Rules:
    ///  - Transition requests are queued and applied at the start and at the end of <see cref="Update"/>
    ///    (never re-entrantly inside Enter/Exit/event handlers).
    ///  - A request is honoured only if its source is the active state. If its source is the
    ///    suspended state (pause), the request is remembered and applied right after <see cref="Resume"/>,
    ///    so an event that arrives while paused is not lost.
    ///  - <see cref="Suspend"/>/<see cref="Resume"/> park a state WITHOUT calling its Exit/Enter.
    ///  - <see cref="EnqueueOverriding"/> drops everything pending (used for game over).
    /// </summary>
    public sealed class StateMachine : IStateChanger
    {
        /// <summary>Safety net against transition loops that resolve synchronously (A -> B -> A ...).</summary>
        public const int MaxCommandsPerFlush = 1000;

        private readonly Queue<Action> _commands = new Queue<Action>();
        private IState _deferredTarget;

        public IState Current { get; private set; }

        /// <summary>The state parked by <see cref="Suspend"/>, or null.</summary>
        public IState Suspended { get; private set; }

        public bool IsRunning
        {
            get { return Current != null; }
        }

        public void Start(IState initial)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (IsRunning) throw new InvalidOperationException("StateMachine is already running.");

            _commands.Clear();
            _deferredTarget = null;
            Current = initial;
            initial.Enter();
            Flush();
        }

        /// <summary>Exits the active state and the suspended one (if any) and stops the machine.</summary>
        public void Stop()
        {
            var active = Current;
            var suspended = Suspended;
            Current = null;
            Suspended = null;
            _deferredTarget = null;

            if (active != null) active.Exit();
            if (suspended != null) suspended.Exit();

            _commands.Clear();
        }

        public void Update(float deltaTime)
        {
            Flush();
            if (Current != null) Current.Update(deltaTime);
            Flush();
        }

        // ---------------------------------------------------------------- requests

        public void ChangeState(IState source, IState target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            _commands.Enqueue(() => ApplyRequest(source, target));
        }

        /// <summary>Queues an arbitrary command executed in order with the other requests.</summary>
        public void Enqueue(Action command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _commands.Enqueue(command);
        }

        /// <summary>Drops every pending request (including deferred ones) and queues this command alone.</summary>
        public void EnqueueOverriding(Action command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _commands.Clear();
            _deferredTarget = null;
            _commands.Enqueue(command);
        }

        // ------------------------------------------------- primitives for commands
        // Meant to be called from inside commands (Enqueue/EnqueueOverriding), not from event handlers.

        /// <summary>Parks the current state (no Exit) and enters <paramref name="suspendingState"/>.</summary>
        public void Suspend(IState suspendingState)
        {
            if (suspendingState == null) throw new ArgumentNullException(nameof(suspendingState));
            if (Current == null) throw new InvalidOperationException("Nothing to suspend.");
            if (Suspended != null) throw new InvalidOperationException("A state is already suspended.");

            Suspended = Current;
            Current = suspendingState;
            suspendingState.Enter();
        }

        /// <summary>Exits the current state and restores the suspended one (no Enter).</summary>
        public void Resume()
        {
            if (Suspended == null) throw new InvalidOperationException("No suspended state to resume.");

            var leaving = Current;
            var restored = Suspended;

            if (leaving != null) leaving.Exit();

            Suspended = null;
            Current = restored;

            if (_deferredTarget != null)
            {
                var target = _deferredTarget;
                _deferredTarget = null;
                _commands.Enqueue(() => ApplyRequest(restored, target));
            }
        }

        /// <summary>Exits the active and the suspended state and enters <paramref name="target"/>.</summary>
        public void ForceChange(IState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var active = Current;
            var suspended = Suspended;
            Current = null;
            Suspended = null;
            _deferredTarget = null;

            if (active != null) active.Exit();
            if (suspended != null) suspended.Exit();

            Current = target;
            target.Enter();
        }

        // ---------------------------------------------------------------- internals

        private void ApplyRequest(IState source, IState target)
        {
            if (ReferenceEquals(source, Current))
            {
                SwitchTo(target);
            }
            else if (Suspended != null && ReferenceEquals(source, Suspended) && _deferredTarget == null)
            {
                _deferredTarget = target; // remembered, applied on Resume
            }
            // anything else: ignored (stale or foreign request)
        }

        private void SwitchTo(IState target)
        {
            var previous = Current;
            if (previous != null) previous.Exit();
            Current = target;
            target.Enter();
        }

        private void Flush()
        {
            int executed = 0;
            while (_commands.Count > 0)
            {
                if (++executed > MaxCommandsPerFlush)
                {
                    _commands.Clear();
                    throw new InvalidOperationException(
                        "StateMachine: more than " + MaxCommandsPerFlush +
                        " transitions in a single tick. Probably a synchronous transition loop.");
                }
                _commands.Dequeue().Invoke();
            }
        }
    }
}
