using System;
using NUnit.Framework;
using TurnGame.Core;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Game
{
    [TestFixture]
    public class GameMachinePauseTests
    {
        private GameMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new GameMachineHarness();
        }

        // pausable state, request event it publishes on Enter (used to detect a second Enter)
        [TestCase(typeof(TurnStartingState),    typeof(TurnStartRequested))]
        [TestCase(typeof(RollingElementsState), typeof(RollElementsRequested))]
        [TestCase(typeof(BuildingPieceState),   typeof(BuildPieceRequested))]
        [TestCase(typeof(PlacingPieceState),    typeof(PlacePieceRequested))]
        [TestCase(typeof(UpdatingBoardState),   typeof(UpdateBoardRequested))]
        [TestCase(typeof(CheckingBoardState),   typeof(CheckBoardRequested))]
        [TestCase(typeof(ResolvingPlayState),   typeof(ResolvePlayRequested))]
        [TestCase(typeof(PlayingFeedbackState), typeof(PlayFeedbackRequested))]
        [TestCase(typeof(TurnEndingState),      typeof(TurnEndRequested))]
        public void Pause_SuspendsTheActiveState_WithoutExitingIt(Type state, Type request)
        {
            _h.GoTo(state);
            int subscribersBefore = _h.Broker.TotalSubscriberCount;

            _h.Send<PauseRequested>();

            Assert.IsInstanceOf<PausedState>(_h.Current);
            Assert.IsInstanceOf(state, _h.Suspended);
            Assert.AreEqual(1, _h.Broker.CountOf<GamePaused>());
            // Exit was not called: the suspended state keeps its subscriptions (+1 = the Paused state itself).
            Assert.AreEqual(subscribersBefore + 1, _h.Broker.TotalSubscriberCount);
        }

        [TestCase(typeof(TurnStartingState),    typeof(TurnStartRequested))]
        [TestCase(typeof(RollingElementsState), typeof(RollElementsRequested))]
        [TestCase(typeof(BuildingPieceState),   typeof(BuildPieceRequested))]
        [TestCase(typeof(PlacingPieceState),    typeof(PlacePieceRequested))]
        [TestCase(typeof(UpdatingBoardState),   typeof(UpdateBoardRequested))]
        [TestCase(typeof(CheckingBoardState),   typeof(CheckBoardRequested))]
        [TestCase(typeof(ResolvingPlayState),   typeof(ResolvePlayRequested))]
        [TestCase(typeof(PlayingFeedbackState), typeof(PlayFeedbackRequested))]
        [TestCase(typeof(TurnEndingState),      typeof(TurnEndRequested))]
        public void Resume_RestoresTheSameInstance_WithoutEnteringAgain(Type state, Type request)
        {
            _h.GoTo(state);
            var original = _h.Current;
            int subscribersBefore = _h.Broker.TotalSubscriberCount;
            int requestsBefore = _h.Broker.CountOf(request);

            _h.Send<PauseRequested>();
            _h.Send<ResumeRequested>();

            Assert.AreSame(original, _h.Current);
            Assert.IsNull(_h.Suspended);
            Assert.AreEqual(requestsBefore, _h.Broker.CountOf(request), "Enter must not run again (the request would be re-published)");
            Assert.AreEqual(subscribersBefore, _h.Broker.TotalSubscriberCount);
            Assert.AreEqual(1, _h.Broker.CountOf<GameResumed>());
        }

        [Test]
        public void AfterResume_TheRestoredStateStillReactsToItsEvent()
        {
            _h.GoTo(typeof(PlacingPieceState));
            _h.Send<PauseRequested>();
            _h.Send<ResumeRequested>();

            _h.Send<PiecePlaced>();

            Assert.IsInstanceOf<UpdatingBoardState>(_h.Current);
        }

        [Test]
        public void PauseAndResume_CanBeRepeated()
        {
            _h.GoTo(typeof(BuildingPieceState));

            for (int i = 0; i < 5; i++)
            {
                _h.Send<PauseRequested>();
                Assert.IsInstanceOf<PausedState>(_h.Current);
                _h.Send<ResumeRequested>();
                Assert.IsInstanceOf<BuildingPieceState>(_h.Current);
            }

            Assert.AreEqual(5, _h.Broker.CountOf<GamePaused>());
            Assert.AreEqual(5, _h.Broker.CountOf<GameResumed>());
            Assert.AreEqual(1, _h.Broker.CountOf<BuildPieceRequested>());
        }

        // ------------------------------------------------ states that cannot be paused

        [TestCase(typeof(PausedState))]
        [TestCase(typeof(InGameOptionsState))]
        [TestCase(typeof(GameOverScreenState))]
        public void Pause_IsIgnored_InPausedOptionsAndGameOver(Type state)
        {
            _h.GoTo(state);
            int pausedBefore = _h.Broker.CountOf<GamePaused>();
            var before = _h.Current;
            var suspendedBefore = _h.Suspended;

            _h.Send<PauseRequested>();

            Assert.AreSame(before, _h.Current);
            Assert.AreSame(suspendedBefore, _h.Suspended);
            Assert.AreEqual(pausedBefore, _h.Broker.CountOf<GamePaused>());
        }

        [Test]
        public void Resume_WithoutPause_IsIgnored()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Send<ResumeRequested>();

            Assert.IsInstanceOf<RollingElementsState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<GameResumed>());
        }

        // ------------------------------------------------ pause <-> in-game options

        [Test]
        public void Paused_OptionsRequested_GoesToInGameOptions_KeepingTheSuspendedState()
        {
            _h.GoTo(typeof(RollingElementsState));
            _h.Send<PauseRequested>();

            _h.Send<OptionsRequested>();

            Assert.IsInstanceOf<InGameOptionsState>(_h.Current);
            Assert.IsInstanceOf<RollingElementsState>(_h.Suspended);
            Assert.AreEqual(1, _h.Broker.CountOf<OptionsScreenRequested>());
        }

        [Test]
        public void InGameOptions_Closed_ReturnsToPaused_NotToTheOriginalState()
        {
            _h.GoTo(typeof(InGameOptionsState));

            _h.Send<OptionsClosed>();

            Assert.IsInstanceOf<PausedState>(_h.Current);
            Assert.IsInstanceOf<RollingElementsState>(_h.Suspended);
        }

        [Test]
        public void Resume_FromInGameOptions_IsIgnored()
        {
            _h.GoTo(typeof(InGameOptionsState));

            _h.Send<ResumeRequested>();

            Assert.IsInstanceOf<InGameOptionsState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<GameResumed>());
        }

        [Test]
        public void FullRoundTrip_Pause_Options_Back_Resume_ReturnsToTheSameState()
        {
            _h.GoTo(typeof(PlacingPieceState));
            var original = _h.Current;

            _h.Send<PauseRequested>();
            _h.Send<OptionsRequested>();
            _h.Send<OptionsClosed>();
            _h.Send<OptionsRequested>();
            _h.Send<OptionsClosed>();
            _h.Send<ResumeRequested>();

            Assert.AreSame(original, _h.Current);
            Assert.AreEqual(1, _h.Broker.CountOf<PlacePieceRequested>());
        }

        [Test]
        public void QuitToMenuRequested_IsNotHandledByGameMachine_PausedStays()
        {
            _h.GoTo(typeof(PausedState));

            _h.Send<QuitToMenuRequested>();

            Assert.IsInstanceOf<PausedState>(_h.Current);
            Assert.IsTrue(_h.Machine.IsInPauseMenu);
        }

        // ------------------------------------------------ events that arrive while paused

        [Test]
        public void EventForTheSuspendedState_WhilePaused_IsNotLost_ItAppliesOnResume()
        {
            _h.GoTo(typeof(RollingElementsState));
            _h.Send<PauseRequested>();

            _h.Send<ElementsRolled>(); // e.g. a dice animation that finishes while paused

            Assert.IsInstanceOf<PausedState>(_h.Current, "game stays frozen");

            _h.Send<ResumeRequested>();

            Assert.IsInstanceOf<BuildingPieceState>(_h.Current);
            Assert.Less(_h.Broker.LastIndexOf<GameResumed>(), _h.Broker.LastIndexOf<BuildPieceRequested>(),
                "GameResumed is announced before the deferred transition runs");
        }

        [Test]
        public void EventForTheSuspendedState_WhileInOptions_AlsoSurvivesUntilResume()
        {
            _h.GoTo(typeof(InGameOptionsState));
            _h.Send<ElementsRolled>();
            _h.Send<OptionsClosed>();
            Assert.IsInstanceOf<PausedState>(_h.Current);

            _h.Send<ResumeRequested>();

            Assert.IsInstanceOf<BuildingPieceState>(_h.Current);
        }

        [Test]
        public void SameTick_EventThenPause_TransitionFirst_ThenPauseTheNewState()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<ElementsRolled>();
            _h.Queue<PauseRequested>();
            _h.Tick();

            Assert.IsInstanceOf<PausedState>(_h.Current);
            Assert.IsInstanceOf<BuildingPieceState>(_h.Suspended);
        }

        [Test]
        public void SameTick_PauseThenEvent_PauseFirst_AndTheEventIsDeferredUntilResume()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<PauseRequested>();
            _h.Queue<ElementsRolled>();
            _h.Tick();

            Assert.IsInstanceOf<PausedState>(_h.Current);
            Assert.IsInstanceOf<RollingElementsState>(_h.Suspended);

            _h.Send<ResumeRequested>();
            Assert.IsInstanceOf<BuildingPieceState>(_h.Current);
        }

        [Test]
        public void DoublePauseRequest_InTheSameTick_OnlyPausesOnce()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<PauseRequested>();
            _h.Queue<PauseRequested>();
            _h.Tick();

            Assert.AreEqual(1, _h.Broker.CountOf<GamePaused>());
            Assert.IsInstanceOf<RollingElementsState>(_h.Suspended);
        }

        // ------------------------------------------------ pause never updates the frozen state

        [Test]
        public void WhilePaused_OnlyThePauseStateIsTheActiveOne()
        {
            _h.GoTo(typeof(BuildingPieceState));
            _h.Send<PauseRequested>();

            for (int i = 0; i < 10; i++) _h.Tick();

            Assert.IsInstanceOf<PausedState>(_h.Current);
            Assert.IsInstanceOf<BuildingPieceState>(_h.Suspended);
        }
    }
}
