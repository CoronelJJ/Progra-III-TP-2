using System.Collections.Generic;
using NUnit.Framework;
using TurnGame.Core;

namespace TurnGame.Tests.Core
{
    [TestFixture]
    public class MessageBrokerTests
    {
        private struct Ping { public int Value; }
        private struct Pong { }

        private MessageBroker _broker;

        [SetUp]
        public void SetUp()
        {
            _broker = new MessageBroker();
        }

        [Test]
        public void Publish_DeliversMessageToSubscriber()
        {
            int received = 0;
            _broker.Subscribe<Ping>(p => received = p.Value);

            _broker.Publish(new Ping { Value = 7 });

            Assert.AreEqual(7, received);
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => _broker.Publish(new Ping()));
        }

        [Test]
        public void Publish_OnlyReachesSubscribersOfThatType()
        {
            int pings = 0, pongs = 0;
            _broker.Subscribe<Ping>(_ => pings++);
            _broker.Subscribe<Pong>(_ => pongs++);

            _broker.Publish(new Ping());

            Assert.AreEqual(1, pings);
            Assert.AreEqual(0, pongs);
        }

        [Test]
        public void Publish_ReachesAllSubscribersInSubscriptionOrder()
        {
            var order = new List<int>();
            _broker.Subscribe<Ping>(_ => order.Add(1));
            _broker.Subscribe<Ping>(_ => order.Add(2));
            _broker.Subscribe<Ping>(_ => order.Add(3));

            _broker.Publish(new Ping());

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int count = 0;
            System.Action<Ping> handler = _ => count++;
            _broker.Subscribe(handler);
            _broker.Publish(new Ping());

            _broker.Unsubscribe(handler);
            _broker.Publish(new Ping());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unsubscribe_OfMethodGroup_Works()
        {
            // States subscribe with method groups; delegate equality must make Unsubscribe work.
            var target = new Counter();
            _broker.Subscribe<Ping>(target.OnPing);
            _broker.Unsubscribe<Ping>(target.OnPing);

            _broker.Publish(new Ping());

            Assert.AreEqual(0, target.Count);
            Assert.AreEqual(0, _broker.SubscriberCount<Ping>());
        }

        [Test]
        public void HandlerRemovedDuringPublish_IsSkipped()
        {
            int secondCalls = 0;
            System.Action<Ping> second = _ => secondCalls++;
            _broker.Subscribe<Ping>(_ => _broker.Unsubscribe(second));
            _broker.Subscribe(second);

            _broker.Publish(new Ping());

            Assert.AreEqual(0, secondCalls);
        }

        [Test]
        public void HandlerAddedDuringPublish_IsNotCalledInTheSameRound()
        {
            int lateCalls = 0;
            _broker.Subscribe<Ping>(_ => _broker.Subscribe<Ping>(__ => lateCalls++));

            _broker.Publish(new Ping());
            Assert.AreEqual(0, lateCalls);

            _broker.Publish(new Ping());
            Assert.AreEqual(1, lateCalls);
        }

        [Test]
        public void SubscriberCounts_AreTracked()
        {
            System.Action<Ping> a = _ => { };
            System.Action<Pong> b = _ => { };
            _broker.Subscribe(a);
            _broker.Subscribe(b);

            Assert.AreEqual(1, _broker.SubscriberCount<Ping>());
            Assert.AreEqual(2, _broker.TotalSubscriberCount);

            _broker.Unsubscribe(a);
            _broker.Unsubscribe(b);

            Assert.AreEqual(0, _broker.TotalSubscriberCount);
        }

        [Test]
        public void Subscribe_Null_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => _broker.Subscribe<Ping>(null));
        }

        [Test]
        public void PublishExtension_WithoutPayload_DeliversDefault()
        {
            int received = -1;
            _broker.Subscribe<Ping>(p => received = p.Value);

            _broker.Publish<Ping>();

            Assert.AreEqual(0, received);
        }

        private sealed class Counter
        {
            public int Count;
            public void OnPing(Ping _) { Count++; }
        }
    }
}
