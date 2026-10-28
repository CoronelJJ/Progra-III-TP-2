using TurnGame.Core;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Game
{
    /// <summary>The transition table of the GameMachine (normal flow), as executable specification.</summary>
    [TestFixture]
    public class GameMachineTransitionTests
    {
        private GameMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new GameMachineHarness();
        }

        [Test]
        public void StartsInTurnStarting()
        {
            Assert.IsInstanceOf<TurnStartingState>(_h.Current);
        }

        // from-state, event, expected state
        [TestCase(typeof(TurnStartingState), typeof(TurnIntroFinished), typeof(RollingElementsState))]
        [TestCase(typeof(RollingElementsState), typeof(ElementsRolled), typeof(BuildingPieceState))]
        [TestCase(typeof(BuildingPieceState), typeof(PieceBuilt), typeof(PlacingPieceState))]
        [TestCase(typeof(PlacingPieceState), typeof(PiecePlaced), typeof(UpdatingBoardState))]
        [TestCase(typeof(UpdatingBoardState), typeof(BoardUpdated), typeof(CheckingBoardState))]
        [TestCase(typeof(CheckingBoardState), typeof(PlayDetected), typeof(ResolvingPlayState))]
        [TestCase(typeof(CheckingBoardState), typeof(NoPlayDetected), typeof(TurnEndingState))]
        [TestCase(typeof(ResolvingPlayState), typeof(PlayResolved), typeof(PlayingFeedbackState))]
        [TestCase(typeof(PlayingFeedbackState), typeof(FeedbackFinished), typeof(CheckingBoardState))]
        [TestCase(typeof(TurnEndingState), typeof(NextTurnReady), typeof(TurnStartingState))]
        public void Event_MovesToExpectedState(Type from, Type trigger, Type expected)
        {
            _h.GoTo(from);
            Assert.IsInstanceOf(from, _h.Current, "precondition");

            _h.Send(trigger);

            Assert.IsInstanceOf(expected, _h.Current);
        }

        // ------------------------------------------------ events out of context: full cross product

        private static readonly Dictionary<Type, Type[]> ValidTriggers = new Dictionary<Type, Type[]>
        {
            { typeof(TurnStartingState),    new[] { typeof(TurnIntroFinished) } },
            { typeof(RollingElementsState), new[] { typeof(ElementsRolled) } },
            { typeof(BuildingPieceState),   new[] { typeof(PieceBuilt) } },
            { typeof(PlacingPieceState),    new[] { typeof(PiecePlaced) } },
            { typeof(UpdatingBoardState),   new[] { typeof(BoardUpdated) } },
            { typeof(CheckingBoardState),   new[] { typeof(PlayDetected), typeof(NoPlayDetected) } },
            { typeof(ResolvingPlayState),   new[] { typeof(PlayResolved) } },
            { typeof(PlayingFeedbackState), new[] { typeof(FeedbackFinished) } },
            { typeof(TurnEndingState),      new[] { typeof(NextTurnReady) } },
        };

        // Everything that might arrive while playing and that a normal state must NOT react to.
        private static readonly Type[] CandidateEvents =
        {
            typeof(TurnIntroFinished), typeof(ElementsRolled), typeof(PieceBuilt), typeof(PiecePlaced),
            typeof(BoardUpdated), typeof(PlayDetected), typeof(NoPlayDetected), typeof(PlayResolved),
            typeof(FeedbackFinished), typeof(NextTurnReady), typeof(PieceMoved),
            typeof(OptionsRequested), typeof(OptionsClosed), typeof(ResumeRequested),
            typeof(GameOverDismissed), typeof(QuitToMenuRequested), typeof(GameFinished),
        };

        public static IEnumerable<object[]> OutOfContextCases()
        {
            foreach (var state in GameMachineHarness.FlowStates)
            {
                foreach (var ev in CandidateEvents)
                {
                    if (Array.IndexOf(ValidTriggers[state], ev) >= 0) continue;
                    yield return new object[] { state, ev };
                }
            }
        }

        [TestCaseSource(nameof(OutOfContextCases))]
        public void EventOutOfContext_IsIgnored(Type state, Type ev)
        {
            _h.GoTo(state);

            _h.Send(ev);

            Assert.IsInstanceOf(state, _h.Current);
        }

        [TestCaseSource(typeof(GameMachineHarness), nameof(GameMachineHarness.FlowStates))]
        public void PieceMoved_NeverChangesTheState_InAnyState(Type state)
        {
            _h.GoTo(state);

            _h.Send(new PieceMoved(1f, 2f, true));
            _h.Send(new PieceMoved(3f, 4f, false));

            Assert.IsInstanceOf(state, _h.Current);
        }

        [Test]
        public void PiecePlaced_OutsidePlacingPiece_IsIgnored()
        {
            _h.GoTo(typeof(BuildingPieceState));

            _h.Send<PiecePlaced>();

            Assert.IsInstanceOf<BuildingPieceState>(_h.Current);
        }

        [Test]
        public void PieceMoved_Stream_ThenPiecePlaced_AdvancesOnlyOnPiecePlaced()
        {
            _h.GoTo(typeof(PlacingPieceState));

            _h.Send(new PieceMoved(0f, 0f, false));
            _h.Send(new PieceMoved(1f, 0f, true));
            Assert.IsInstanceOf<PlacingPieceState>(_h.Current);

            _h.Send<PiecePlaced>();
            Assert.IsInstanceOf<UpdatingBoardState>(_h.Current);
        }

        // ------------------------------------------------ requests published on Enter

        [Test]
        public void EachStatePublishesItsRequest_WhenEntered()
        {
            Assert.AreEqual(1, _h.Broker.CountOf<TurnStartRequested>());
            _h.Send<TurnIntroFinished>();
            Assert.AreEqual(1, _h.Broker.CountOf<RollElementsRequested>());
            _h.Send<ElementsRolled>();
            Assert.AreEqual(1, _h.Broker.CountOf<BuildPieceRequested>());
            _h.Send<PieceBuilt>();
            Assert.AreEqual(1, _h.Broker.CountOf<PlacePieceRequested>());
            _h.Send<PiecePlaced>();
            Assert.AreEqual(1, _h.Broker.CountOf<UpdateBoardRequested>());
            _h.Send<BoardUpdated>();
            Assert.AreEqual(1, _h.Broker.CountOf<CheckBoardRequested>());
            _h.Send<PlayDetected>();
            Assert.AreEqual(1, _h.Broker.CountOf<ResolvePlayRequested>());
            _h.Send<PlayResolved>();
            Assert.AreEqual(1, _h.Broker.CountOf<PlayFeedbackRequested>());
            _h.Send<FeedbackFinished>();
            Assert.AreEqual(2, _h.Broker.CountOf<CheckBoardRequested>());
            _h.Send<NoPlayDetected>();
            Assert.AreEqual(1, _h.Broker.CountOf<TurnEndRequested>());
        }

        // ------------------------------------------------ state is applied on the tick, not inside the handler

        [Test]
        public void TransitionIsAppliedAtTheNextTick_NotInsideTheEventHandler()
        {
            _h.Broker.Publish<TurnIntroFinished>();
            Assert.IsInstanceOf<TurnStartingState>(_h.Current, "not applied yet");

            _h.Tick();

            Assert.IsInstanceOf<RollingElementsState>(_h.Current);
        }

        [Test]
        public void TwoEventsInTheSameTick_OnlyTheFirstApplies_TheSecondIsStale()
        {
            _h.GoTo(typeof(RollingElementsState));

            _h.Broker.Publish<ElementsRolled>();
            _h.Broker.Publish<PieceBuilt>(); // arrives before the machine left RollingElements: nobody listens
            _h.Tick();

            Assert.IsInstanceOf<BuildingPieceState>(_h.Current);
        }

        // ------------------------------------------------ no leaks

        [TestCaseSource(typeof(GameMachineHarness), nameof(GameMachineHarness.FlowStates))]
        public void Stop_LeavesNoSubscriptions(Type state)
        {
            _h.GoTo(state);

            _h.Machine.Stop();

            Assert.AreEqual(0, _h.Broker.TotalSubscriberCount);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _h.Machine.Start());
        }
    }
}
