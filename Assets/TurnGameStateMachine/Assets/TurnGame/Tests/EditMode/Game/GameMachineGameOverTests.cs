using System;
using NUnit.Framework;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Game
{
    /// <summary>GameOverReached is a global transition, like pause: any state, any moment.</summary>
    [TestFixture]
    public class GameMachineGameOverTests
    {
        private GameMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new GameMachineHarness();
        }

        [TestCase(typeof(TurnStartingState))]
        [TestCase(typeof(RollingElementsState))]
        [TestCase(typeof(BuildingPieceState))]
        [TestCase(typeof(PlacingPieceState))]
        [TestCase(typeof(UpdatingBoardState))]
        [TestCase(typeof(CheckingBoardState))]
        [TestCase(typeof(ResolvingPlayState))]
        [TestCase(typeof(PlayingFeedbackState))]
        [TestCase(typeof(TurnEndingState))]
        [TestCase(typeof(PausedState))]
        [TestCase(typeof(InGameOptionsState))]
        public void GameOverReached_FromAnyState_GoesToGameOverScreen(Type from)
        {
            _h.GoTo(from);

            _h.Send<GameOverReached>();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.IsNull(_h.Suspended);
            Assert.AreEqual(1, _h.Broker.CountOf<GameOverScreenRequested>());
        }

        [TestCase(typeof(TurnStartingState))]
        [TestCase(typeof(RollingElementsState))]
        [TestCase(typeof(BuildingPieceState))]
        [TestCase(typeof(PlacingPieceState))]
        [TestCase(typeof(UpdatingBoardState))]
        [TestCase(typeof(CheckingBoardState))]
        [TestCase(typeof(ResolvingPlayState))]
        [TestCase(typeof(PlayingFeedbackState))]
        [TestCase(typeof(TurnEndingState))]
        [TestCase(typeof(PausedState))]
        [TestCase(typeof(InGameOptionsState))]
        public void GameOver_ExitsActiveAndSuspendedStates_LeavingNothingSubscribed(Type from)
        {
            _h.GoTo(from);

            _h.Send<GameOverReached>();
            _h.Machine.Stop(); // exits GameOverScreen and removes the global handlers

            Assert.AreEqual(0, _h.Broker.TotalSubscriberCount);
        }

        [Test]
        public void GameOver_FromPlaying_OldStateNoLongerReactsToItsEvent()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Send<GameOverReached>();
            _h.Send<ElementsRolled>();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<BuildPieceRequested>());
        }

        // ------------------------------------------------ priority inside a tick

        [Test]
        public void SameTick_PendingTransitionThenGameOver_GameOverWins()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<ElementsRolled>();
            _h.Queue<GameOverReached>();
            _h.Tick();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<BuildPieceRequested>(), "the dropped transition never ran");
        }

        [Test]
        public void SameTick_PauseThenGameOver_GameOverWins()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<PauseRequested>();
            _h.Queue<GameOverReached>();
            _h.Tick();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<GamePaused>());
        }

        [Test]
        public void SameTick_GameOverThenPause_GameOverWins()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<GameOverReached>();
            _h.Queue<PauseRequested>();
            _h.Tick();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<GamePaused>());
        }

        [Test]
        public void SameTick_GameOverThenNormalEvent_GameOverWins()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Queue<GameOverReached>();
            _h.Queue<ElementsRolled>();
            _h.Tick();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
        }

        [Test]
        public void SameTick_GameOverWhilePaused_WithAPendingDeferredEvent_GameOverWins()
        {
            _h.GoTo(typeof(RollingElementsState));
            _h.Send<PauseRequested>();
            _h.Send<ElementsRolled>(); // deferred for the suspended state

            _h.Send<GameOverReached>();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.AreEqual(0, _h.Broker.CountOf<BuildPieceRequested>());
        }

        // ------------------------------------------------ GameOverScreen itself

        [Test]
        public void GameOverReached_WhenAlreadyInGameOverScreen_IsIgnored()
        {
            _h.GoTo(typeof(GameOverScreenState));

            _h.Send<GameOverReached>();
            _h.Send<GameOverReached>();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
            Assert.AreEqual(1, _h.Broker.CountOf<GameOverScreenRequested>());
        }

        [Test]
        public void GameOverReached_TwiceInTheSameTick_EntersTheScreenOnce()
        {
            _h.GoTo(typeof(BuildingPieceState));

            _h.Queue<GameOverReached>();
            _h.Queue<GameOverReached>();
            _h.Tick();

            Assert.AreEqual(1, _h.Broker.CountOf<GameOverScreenRequested>());
        }

        [Test]
        public void Dismissed_PublishesGameFinished_AndStaysUntilInGameReactsToIt()
        {
            _h.GoTo(typeof(GameOverScreenState));

            _h.Send<GameOverDismissed>();

            Assert.AreEqual(1, _h.Broker.CountOf<GameFinished>());
            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
        }

        [Test]
        public void Dismissed_Twice_PublishesGameFinishedOnce()
        {
            _h.GoTo(typeof(GameOverScreenState));

            _h.Send<GameOverDismissed>();
            _h.Send<GameOverDismissed>();

            Assert.AreEqual(1, _h.Broker.CountOf<GameFinished>());
        }

        [Test]
        public void GameOverScreen_IgnoresNormalFlowEvents()
        {
            _h.GoTo(typeof(GameOverScreenState));

            _h.Send<ElementsRolled>();
            _h.Send<PiecePlaced>();
            _h.Send<NextTurnReady>();
            _h.Send<ResumeRequested>();
            _h.Send<OptionsRequested>();

            Assert.IsInstanceOf<GameOverScreenState>(_h.Current);
        }
    }
}
