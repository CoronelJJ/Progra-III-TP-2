using System;
using System.Collections.Generic;
using DiceMaterials;
using NUnit.Framework;
using TurnGame.Core;

namespace TurnGame.Persistence.Tests
{
    public class SaveLoadMessageAdapterTests
    {
        private sealed class FakeSaveService : ISaveService
        {
            public readonly List<string> Saved = new List<string>();
            public int Applied;
            public Exception SaveThrows, TryReadThrows, ApplyThrows;
            public bool SlotExists = true;

            public bool Exists(string slot) => SlotExists;
            public void Save(string slot) { if (SaveThrows != null) throw SaveThrows; Saved.Add(slot); }
            public bool TryRead(string slot, out GameSaveData data)
            {
                if (TryReadThrows != null) throw TryReadThrows;
                data = SlotExists ? new GameSaveData() : null;
                return SlotExists;
            }
            public void Apply(GameSaveData data) { if (ApplyThrows != null) throw ApplyThrows; Applied++; }
            public void Delete(string slot) { }
        }

        private sealed class Rig
        {
            public readonly MessageBroker Broker = new MessageBroker();
            public readonly FakeSaveService Saves = new FakeSaveService();
            public readonly SaveLoadMessageAdapter Adapter;
            public readonly List<GameSaved> Saved = new List<GameSaved>();
            public readonly List<SaveFailed> SaveFailures = new List<SaveFailed>();
            public readonly List<GameLoaded> Loaded = new List<GameLoaded>();
            public readonly List<LoadFailed> LoadFailures = new List<LoadFailed>();

            public Rig()
            {
                Adapter = new SaveLoadMessageAdapter(Broker, Saves);
                Adapter.Initialize();
                Broker.Subscribe<GameSaved>(m => Saved.Add(m));
                Broker.Subscribe<SaveFailed>(m => SaveFailures.Add(m));
                Broker.Subscribe<GameLoaded>(m => Loaded.Add(m));
                Broker.Subscribe<LoadFailed>(m => LoadFailures.Add(m));
            }
        }

        [Test]
        public void SaveRequested_Saves_AndPublishesGameSaved()
        {
            var rig = new Rig();
            rig.Broker.Publish(new SaveRequested("slot1"));

            CollectionAssert.AreEqual(new[] { "slot1" }, rig.Saves.Saved);
            Assert.AreEqual(1, rig.Saved.Count);
            Assert.AreEqual("slot1", rig.Saved[0].Slot);
            Assert.AreEqual(0, rig.SaveFailures.Count);
        }

        [Test]
        public void SaveRequested_WhenSavingThrows_PublishesSaveFailed_WithTheMessage()
        {
            var rig = new Rig();
            rig.Saves.SaveThrows = new InvalidOperationException("disco lleno");
            rig.Broker.Publish(new SaveRequested("slot1"));

            Assert.AreEqual(0, rig.Saved.Count);
            Assert.AreEqual(1, rig.SaveFailures.Count);
            Assert.AreEqual("slot1", rig.SaveFailures[0].Slot);
            Assert.IsTrue(rig.SaveFailures[0].Error.Contains("disco lleno"));
        }

        [Test]
        public void LoadRequested_ReadsApplies_AndPublishesGameLoaded()
        {
            var rig = new Rig();
            rig.Broker.Publish(new LoadRequested("slot1"));

            Assert.AreEqual(1, rig.Saves.Applied);
            Assert.AreEqual(1, rig.Loaded.Count);
            Assert.AreEqual("slot1", rig.Loaded[0].Slot);
            Assert.AreEqual(0, rig.LoadFailures.Count);
        }

        [Test]
        public void LoadRequested_MissingSlot_PublishesLoadFailed_AndAppliesNothing()
        {
            var rig = new Rig();
            rig.Saves.SlotExists = false;
            rig.Broker.Publish(new LoadRequested("nope"));

            Assert.AreEqual(0, rig.Saves.Applied);
            Assert.AreEqual(0, rig.Loaded.Count);
            Assert.AreEqual(1, rig.LoadFailures.Count);
            Assert.AreEqual("nope", rig.LoadFailures[0].Slot);
        }

        [Test]
        public void LoadRequested_WhenApplyThrows_PublishesLoadFailed_WithTheMessage()
        {
            var rig = new Rig();
            rig.Saves.ApplyThrows = new InvalidOperationException("la seed del guardado no coincide");
            rig.Broker.Publish(new LoadRequested("slot1"));

            Assert.AreEqual(0, rig.Loaded.Count);
            Assert.AreEqual(1, rig.LoadFailures.Count);
            Assert.IsTrue(rig.LoadFailures[0].Error.Contains("no coincide"));
        }

        [Test]
        public void LoadRequested_WhenReadingThrows_PublishesLoadFailed()
        {
            var rig = new Rig();
            rig.Saves.TryReadThrows = new FormatException("json corrupto");
            rig.Broker.Publish(new LoadRequested("slot1"));

            Assert.AreEqual(1, rig.LoadFailures.Count);
            Assert.IsTrue(rig.LoadFailures[0].Error.Contains("json corrupto"));
        }

        [Test]
        public void ExceptionInAResultSubscriber_IsNotReportedAsASaveFailure()
        {
            var rig = new Rig();
            rig.Broker.Subscribe<GameSaved>(_ => throw new InvalidOperationException("bug en un suscriptor"));

            Assert.Throws<InvalidOperationException>(() => rig.Broker.Publish(new SaveRequested("slot1")));
            Assert.AreEqual(0, rig.SaveFailures.Count); // el guardado SÍ funcionó
            Assert.AreEqual(1, rig.Saves.Saved.Count);
        }

        [Test]
        public void Dispose_Unsubscribes_AndStopsReacting()
        {
            var rig = new Rig();
            Assert.AreEqual(1, rig.Broker.SubscriberCount<SaveRequested>());
            Assert.AreEqual(1, rig.Broker.SubscriberCount<LoadRequested>());

            rig.Adapter.Dispose();
            Assert.AreEqual(0, rig.Broker.SubscriberCount<SaveRequested>());
            Assert.AreEqual(0, rig.Broker.SubscriberCount<LoadRequested>());

            rig.Broker.Publish(new SaveRequested("slot1"));
            Assert.AreEqual(0, rig.Saves.Saved.Count);

            rig.Adapter.Dispose(); // idempotente
        }

        [Test]
        public void Initialize_Twice_DoesNotSubscribeTwice()
        {
            var rig = new Rig();
            rig.Adapter.Initialize();
            Assert.AreEqual(1, rig.Broker.SubscriberCount<SaveRequested>());

            rig.Broker.Publish(new SaveRequested("slot1"));
            Assert.AreEqual(1, rig.Saves.Saved.Count);
        }

        [Test]
        public void NullDependencies_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveLoadMessageAdapter(null, new FakeSaveService()));
            Assert.Throws<ArgumentNullException>(() => new SaveLoadMessageAdapter(new MessageBroker(), null));
        }
    }
}
