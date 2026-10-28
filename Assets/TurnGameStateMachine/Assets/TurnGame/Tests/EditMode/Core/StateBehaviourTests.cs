using System;
using NUnit.Framework;
using TurnGame.Core;
using TurnGame.Flow.Config;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;
using TurnGame.Flow.Main;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Core
{
    /// <summary>
    /// States tested in isolation with a FakeStateChanger: they only depend on IStateChanger/IMessageBroker.
    /// </summary>
    [TestFixture]
    public class StateBehaviourTests
    {
        private RecordingBroker _broker;
        private FakeStateChanger _changer;
        private SpyState _next;

        [SetUp]
        public void SetUp()
        {
            _broker = new RecordingBroker();
            _changer = new FakeStateChanger();
            _next = new SpyState("Next");
        }

        [Test]
        public void TransitionState_WithoutSetNext_ThrowsOnEnter()
        {
            var state = new RollingElementsState(_changer, _broker);
            Assert.Throws<InvalidOperationException>(() => state.Enter());
        }

        [Test]
        public void TransitionState_OnTrigger_RequestsTransitionToNext()
        {
            var state = new RollingElementsState(_changer, _broker);
            state.SetNext(_next);
            state.Enter();

            _broker.Publish<ElementsRolled>();

            Assert.AreEqual(1, _changer.Requests.Count);
            Assert.AreSame(state, _changer.Requests[0].Source);
            Assert.AreSame(_next, _changer.Requests[0].Target);
        }

        [Test]
        public void TransitionState_Enter_PublishesItsRequest()
        {
            var state = new RollingElementsState(_changer, _broker);
            state.SetNext(_next);

            state.Enter();

            Assert.AreEqual(1, _broker.CountOf<RollElementsRequested>());
        }

        [Test]
        public void TransitionState_SubscribesBeforePublishing_SoSynchronousAnswersAreNotLost()
        {
            var state = new RollingElementsState(_changer, _broker);
            state.SetNext(_next);
            _broker.Subscribe<RollElementsRequested>(_ => _broker.Publish<ElementsRolled>());

            state.Enter();

            Assert.AreEqual(1, _changer.Requests.Count);
        }

        [Test]
        public void TransitionState_Exit_Unsubscribes()
        {
            var state = new RollingElementsState(_changer, _broker);
            state.SetNext(_next);
            state.Enter();

            state.Exit();
            _broker.Publish<ElementsRolled>();

            Assert.AreEqual(0, _broker.TotalSubscriberCount);
            Assert.AreEqual(0, _changer.Requests.Count);
        }

        [Test]
        public void MultiEventState_Exit_UnsubscribesEverything()
        {
            var state = new CheckingBoardState(_changer, _broker);
            state.SetNext(new SpyState("Resolve"), new SpyState("End"));
            state.Enter();
            Assert.AreEqual(2, _broker.TotalSubscriberCount);

            state.Exit();

            Assert.AreEqual(0, _broker.TotalSubscriberCount);
        }

        [Test]
        public void MainMenuState_EachEventGoesToItsOwnDestination()
        {
            var setup = new SpyState("Setup");
            var options = new SpyState("Options");
            var quit = new SpyState("Quit");
            var state = new MainMenuState(_changer, _broker);
            state.SetNext(setup, options, quit);
            state.Enter();

            _broker.Publish<PlayRequested>();
            _broker.Publish<OptionsRequested>();
            _broker.Publish<QuitRequested>();

            Assert.AreSame(setup, _changer.Requests[0].Target);
            Assert.AreSame(options, _changer.Requests[1].Target);
            Assert.AreSame(quit, _changer.Requests[2].Target);
        }

        [Test]
        public void QuittingState_CallsApplicationQuitter()
        {
            var quitter = new FakeApplicationQuitter();
            var state = new QuittingState(_changer, _broker, quitter);

            state.Enter();

            Assert.AreEqual(1, quitter.QuitCount);
        }

        [Test]
        public void InGameState_WithoutConfigInSession_RefusesToEnter()
        {
            var state = new InGameState(_changer, _broker, new GameSession());
            state.SetNext(_next);

            Assert.Throws<InvalidOperationException>(() => state.Enter());
            Assert.AreEqual(0, _broker.TotalSubscriberCount, "a refused Enter must not leave subscriptions behind");
        }

        [Test]
        public void GameOverScreenState_Dismissed_PublishesGameFinishedOnlyOnce()
        {
            var state = new GameOverScreenState(_changer, _broker);
            state.Enter();

            _broker.Publish<GameOverDismissed>();
            _broker.Publish<GameOverDismissed>();

            Assert.AreEqual(1, _broker.CountOf<GameFinished>());
            Assert.AreEqual(0, _changer.Requests.Count, "GameOverScreen never transitions by itself");
        }

        [Test]
        public void States_RejectNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new RollingElementsState(null, _broker));
            Assert.Throws<ArgumentNullException>(() => new RollingElementsState(_changer, null));
        }
    }
}
