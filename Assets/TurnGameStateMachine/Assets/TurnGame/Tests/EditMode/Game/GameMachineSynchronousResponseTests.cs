using TurnGame.Core;
using NUnit.Framework;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Game
{
    /// <summary>
    /// Systems may answer a request synchronously (inside the same publish). Because every state
    /// subscribes before publishing, and transitions are deferred, nothing is lost and nothing re-enters.
    /// </summary>
    [TestFixture]
    public class GameMachineSynchronousResponseTests
    {
        [Test]
        public void SynchronousAnswer_ToCheckBoardRequested_IsNotLost()
        {
            var h = new GameMachineHarness();
            h.Broker.Subscribe<CheckBoardRequested>(_ => h.Broker.Publish<NoPlayDetected>());
            h.GoTo(typeof(UpdatingBoardState));

            h.Send<BoardUpdated>();

            Assert.IsInstanceOf<TurnEndingState>(h.Current);
        }

        [Test]
        public void FullyAutomaticTurn_ResolvesInsideStart_AndEndsAtTurnEnding()
        {
            var h = new GameMachineHarness(start: false);
            h.Broker.Subscribe<TurnStartRequested>(_ => h.Broker.Publish<TurnIntroFinished>());
            h.Broker.Subscribe<RollElementsRequested>(_ => h.Broker.Publish<ElementsRolled>());
            h.Broker.Subscribe<BuildPieceRequested>(_ => h.Broker.Publish<PieceBuilt>());
            h.Broker.Subscribe<PlacePieceRequested>(_ => h.Broker.Publish<PiecePlaced>());
            h.Broker.Subscribe<UpdateBoardRequested>(_ => h.Broker.Publish<BoardUpdated>());
            h.Broker.Subscribe<CheckBoardRequested>(_ => h.Broker.Publish<NoPlayDetected>());
            // TurnEndRequested has no responder: the chain stops there.

            h.Machine.Start();

            Assert.IsInstanceOf<TurnEndingState>(h.Current);
            Assert.AreEqual(1, h.Broker.CountOf<RollElementsRequested>());
            Assert.AreEqual(1, h.Broker.CountOf<PlacePieceRequested>());
            Assert.AreEqual(1, h.Broker.CountOf<TurnEndRequested>());
        }

        [Test]
        public void EndlessSynchronousTurnLoop_IsDetectedInsteadOfFreezingTheGame()
        {
            var h = new GameMachineHarness(start: false);
            h.Broker.Subscribe<TurnStartRequested>(_ => h.Broker.Publish<TurnIntroFinished>());
            h.Broker.Subscribe<RollElementsRequested>(_ => h.Broker.Publish<ElementsRolled>());
            h.Broker.Subscribe<BuildPieceRequested>(_ => h.Broker.Publish<PieceBuilt>());
            h.Broker.Subscribe<PlacePieceRequested>(_ => h.Broker.Publish<PiecePlaced>());
            h.Broker.Subscribe<UpdateBoardRequested>(_ => h.Broker.Publish<BoardUpdated>());
            h.Broker.Subscribe<CheckBoardRequested>(_ => h.Broker.Publish<NoPlayDetected>());
            h.Broker.Subscribe<TurnEndRequested>(_ => h.Broker.Publish<NextTurnReady>());

            Assert.Throws<System.InvalidOperationException>(() => h.Machine.Start());
        }
    }
}
