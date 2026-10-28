using NUnit.Framework;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Game
{
    /// <summary>CheckingBoard cycle (stacked dice chain plays) and the infinite turn loop.</summary>
    [TestFixture]
    public class GameMachineCycleTests
    {
        private GameMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new GameMachineHarness();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(10)]
        public void CheckingBoard_WithNChainedPlays_LoopsNTimes_ThenEndsTheTurn(int plays)
        {
            _h.GoTo(typeof(CheckingBoardState));

            for (int i = 0; i < plays; i++)
            {
                _h.Send<PlayDetected>();
                Assert.IsInstanceOf<ResolvingPlayState>(_h.Current, "play " + i);
                _h.Send<PlayResolved>();
                Assert.IsInstanceOf<PlayingFeedbackState>(_h.Current, "play " + i);
                _h.Send<FeedbackFinished>();
                Assert.IsInstanceOf<CheckingBoardState>(_h.Current, "play " + i);
            }

            _h.Send<NoPlayDetected>();

            Assert.IsInstanceOf<TurnEndingState>(_h.Current);
            Assert.AreEqual(plays + 1, _h.Broker.CountOf<CheckBoardRequested>());
            Assert.AreEqual(plays, _h.Broker.CountOf<ResolvePlayRequested>());
            Assert.AreEqual(plays, _h.Broker.CountOf<PlayFeedbackRequested>());
        }

        [Test]
        public void Turns_RepeatIndefinitely_WhileThereIsNoGameOver()
        {
            const int turns = 25;

            for (int turn = 0; turn < turns; turn++)
            {
                _h.Send<TurnIntroFinished>();
                _h.Send<ElementsRolled>();
                _h.Send<PieceBuilt>();
                _h.Send<PiecePlaced>();
                _h.Send<BoardUpdated>();
                _h.Send<NoPlayDetected>();
                Assert.IsInstanceOf<TurnEndingState>(_h.Current, "turn " + turn);
                _h.Send<NextTurnReady>();
                Assert.IsInstanceOf<TurnStartingState>(_h.Current, "turn " + turn);
            }

            Assert.AreEqual(turns + 1, _h.Broker.CountOf<TurnStartRequested>());
        }

        [Test]
        public void SubscriptionCount_DoesNotGrowAcrossManyCycles()
        {
            _h.GoTo(typeof(CheckingBoardState));
            int baseline = _h.Broker.TotalSubscriberCount;

            for (int i = 0; i < 20; i++)
            {
                _h.Send<PlayDetected>();
                _h.Send<PlayResolved>();
                _h.Send<FeedbackFinished>();
            }

            Assert.AreEqual(baseline, _h.Broker.TotalSubscriberCount);
        }
    }
}
