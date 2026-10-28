using System;
using System.Collections.Generic;
using NUnit.Framework;
using TurnGame.Core;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Core
{
    [TestFixture]
    public class StateMachineTests
    {
        private StateMachine _machine;
        private List<string> _log;
        private SpyState _a, _b, _c, _pause;

        [SetUp]
        public void SetUp()
        {
            _machine = new StateMachine();
            _log = new List<string>();
            _a = new SpyState("A", _log);
            _b = new SpyState("B", _log);
            _c = new SpyState("C", _log);
            _pause = new SpyState("Pause", _log);
        }

        // ------------------------------------------------------------ start / update

        [Test]
        public void Start_EntersInitialState()
        {
            _machine.Start(_a);

            Assert.AreSame(_a, _machine.Current);
            Assert.AreEqual(1, _a.EnterCount);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            _machine.Start(_a);
            Assert.Throws<InvalidOperationException>(() => _machine.Start(_b));
        }

        [Test]
        public void Update_PassesDeltaTimeToActiveState()
        {
            _machine.Start(_a);

            _machine.Update(0.5f);

            Assert.AreEqual(1, _a.UpdateCount);
            Assert.AreEqual(0.5f, _a.LastDeltaTime);
        }

        // ------------------------------------------------------------ transitions

        [Test]
        public void ChangeState_IsNotAppliedUntilNextUpdate()
        {
            _machine.Start(_a);

            _machine.ChangeState(_a, _b);
            Assert.AreSame(_a, _machine.Current);

            _machine.Update(0f);
            Assert.AreSame(_b, _machine.Current);
        }

        [Test]
        public void ChangeState_ExitsOldStateBeforeEnteringNewOne()
        {
            _machine.Start(_a);
            _machine.ChangeState(_a, _b);
            _machine.Update(0f);

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, _log);
        }

        [Test]
        public void ChangeState_FromNonActiveState_IsIgnored()
        {
            _machine.Start(_a);

            _machine.ChangeState(_b, _c);
            _machine.Update(0f);

            Assert.AreSame(_a, _machine.Current);
            Assert.AreEqual(0, _c.EnterCount);
        }

        [Test]
        public void ChangeState_DuplicatedRequestsFromSameState_OnlyFirstApplies()
        {
            _machine.Start(_a);

            _machine.ChangeState(_a, _b);
            _machine.ChangeState(_a, _c);
            _machine.Update(0f);

            Assert.AreSame(_b, _machine.Current);
            Assert.AreEqual(0, _c.EnterCount);
        }

        [Test]
        public void ChangeState_RequestedInsideEnter_IsAppliedAfterEnterReturns()
        {
            _a.OnEnter = () => _machine.ChangeState(_a, _b);

            _machine.Start(_a);

            Assert.AreSame(_b, _machine.Current);
            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, _log);
        }

        [Test]
        public void ChangeState_Chain_ResolvesWithinASingleTick()
        {
            _a.OnEnter = () => _machine.ChangeState(_a, _b);
            _b.OnEnter = () => _machine.ChangeState(_b, _c);

            _machine.Start(_a);

            Assert.AreSame(_c, _machine.Current);
        }

        [Test]
        public void SynchronousTransitionLoop_ThrowsInsteadOfFreezing()
        {
            _a.OnEnter = () => _machine.ChangeState(_a, _b);
            _b.OnEnter = () => _machine.ChangeState(_b, _a);

            Assert.Throws<InvalidOperationException>(() => _machine.Start(_a));
        }

        [Test]
        public void ChangeState_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => _machine.ChangeState(null, _a));
            Assert.Throws<ArgumentNullException>(() => _machine.ChangeState(_a, null));
        }

        // ------------------------------------------------------------ suspension

        [Test]
        public void Suspend_ParksCurrentStateWithoutExit()
        {
            _machine.Start(_a);

            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            Assert.AreSame(_pause, _machine.Current);
            Assert.AreSame(_a, _machine.Suspended);
            Assert.AreEqual(0, _a.ExitCount);
            Assert.AreEqual(1, _pause.EnterCount);
        }

        [Test]
        public void Resume_RestoresSuspendedStateWithoutEnter_AndExitsThePauseState()
        {
            _machine.Start(_a);
            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            _machine.Enqueue(() => _machine.Resume());
            _machine.Update(0f);

            Assert.AreSame(_a, _machine.Current);
            Assert.IsNull(_machine.Suspended);
            Assert.AreEqual(1, _a.EnterCount, "A must not be re-entered");
            Assert.AreEqual(0, _a.ExitCount, "A must never be exited");
            Assert.AreEqual(1, _pause.ExitCount);
        }

        [Test]
        public void Suspend_WhenAlreadySuspended_Throws()
        {
            _machine.Start(_a);
            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            Assert.Throws<InvalidOperationException>(() => _machine.Suspend(_b));
        }

        [Test]
        public void Resume_WithoutSuspendedState_Throws()
        {
            _machine.Start(_a);
            Assert.Throws<InvalidOperationException>(() => _machine.Resume());
        }

        [Test]
        public void RequestFromSuspendedState_IsDeferredUntilResume()
        {
            _machine.Start(_a);
            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            _machine.ChangeState(_a, _b); // A is suspended: must not be lost, must not apply yet
            _machine.Update(0f);
            Assert.AreSame(_pause, _machine.Current);
            Assert.AreEqual(0, _b.EnterCount);

            _machine.Enqueue(() => _machine.Resume());
            _machine.Update(0f);

            Assert.AreSame(_b, _machine.Current);
            Assert.AreEqual(1, _a.ExitCount, "A exits normally when its deferred transition applies");
        }

        [Test]
        public void TransitionBetweenPauseStates_KeepsTheSuspendedStateParked()
        {
            var options = new SpyState("Options", _log);
            _machine.Start(_a);
            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            _machine.ChangeState(_pause, options);
            _machine.Update(0f);

            Assert.AreSame(options, _machine.Current);
            Assert.AreSame(_a, _machine.Suspended);
            Assert.AreEqual(1, _pause.ExitCount);
            Assert.AreEqual(0, _a.ExitCount);
        }

        // ------------------------------------------------------------ force / override / stop

        [Test]
        public void ForceChange_ExitsActiveAndSuspendedAndEntersTarget()
        {
            _machine.Start(_a);
            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            _machine.Enqueue(() => _machine.ForceChange(_b));
            _machine.Update(0f);

            Assert.AreSame(_b, _machine.Current);
            Assert.IsNull(_machine.Suspended);
            Assert.AreEqual(1, _pause.ExitCount);
            Assert.AreEqual(1, _a.ExitCount);
            Assert.AreEqual(1, _b.EnterCount);
        }

        [Test]
        public void EnqueueOverriding_DropsEverythingPending()
        {
            _machine.Start(_a);

            _machine.ChangeState(_a, _b);
            _machine.EnqueueOverriding(() => _machine.ForceChange(_c));
            _machine.Update(0f);

            Assert.AreSame(_c, _machine.Current);
            Assert.AreEqual(0, _b.EnterCount);
        }

        [Test]
        public void Stop_ExitsActiveAndSuspendedStates()
        {
            _machine.Start(_a);
            _machine.Enqueue(() => _machine.Suspend(_pause));
            _machine.Update(0f);

            _machine.Stop();

            Assert.IsFalse(_machine.IsRunning);
            Assert.AreEqual(1, _a.ExitCount);
            Assert.AreEqual(1, _pause.ExitCount);
        }

        [Test]
        public void Stop_DiscardsPendingRequests()
        {
            _machine.Start(_a);
            _machine.ChangeState(_a, _b);

            _machine.Stop();
            _machine.Update(0f);

            Assert.AreEqual(0, _b.EnterCount);
        }
    }
}
