using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class SaveServiceTests
    {
        private sealed class CountingContributor : ISaveContributor
        {
            public int Captures, Restores;
            public void Capture(GameSaveData data) => Captures++;
            public void Restore(GameSaveData data) => Restores++;
        }

        private static SaveService Service(ISaveStorage storage, params ISaveContributor[] contributors)
            => new SaveService(contributors, new FakeSaveSerializer(), storage);

        [Test]
        public void Save_WritesTheSlot_AndExistsReportsIt()
        {
            var storage = new FakeSaveStorage();
            var service = Service(storage);
            Assert.IsFalse(service.Exists("a"));
            service.Save("a");
            Assert.IsTrue(service.Exists("a"));
            service.Delete("a");
            Assert.IsFalse(service.Exists("a"));
        }

        [Test]
        public void TryRead_MissingSlot_ReturnsFalse()
        {
            Assert.IsFalse(Service(new FakeSaveStorage()).TryRead("nope", out var data));
            Assert.IsTrue(data == null);
        }

        [Test]
        public void EveryContributor_IsCapturedOnSave_AndRestoredOnApply()
        {
            var c1 = new CountingContributor();
            var c2 = new CountingContributor();
            var service = Service(new FakeSaveStorage(), c1, c2);

            service.Save("s");
            Assert.IsTrue(service.TryRead("s", out var data));
            service.Apply(data);

            Assert.AreEqual(1, c1.Captures); Assert.AreEqual(1, c2.Captures);
            Assert.AreEqual(1, c1.Restores); Assert.AreEqual(1, c2.Restores);
        }

        [Test]
        public void DiceSystem_SaveThenLoad_ContinuesTheSameSequence()
        {
            var storage = new FakeSaveStorage();

            var live = RollRig.Build(2026UL);
            var liveService = Service(storage, new DiceSystemSaveContributor(live));
            for (int i = 0; i < 5; i++) RollRig.Turn(live, i % 2 == 0 ? 6 : 4);
            liveService.Save("slot1");

            // "Reabrir el juego": sistema nuevo con la misma seed y contenido
            var reloaded = RollRig.Build(2026UL);
            var reloadService = Service(storage, new DiceSystemSaveContributor(reloaded));
            Assert.IsTrue(reloadService.TryRead("slot1", out var data));
            reloadService.Apply(data);

            var a = Enumerable.Range(0, 8).Select(_ => RollRig.Turn(live, 6)).ToList();
            var b = Enumerable.Range(0, 8).Select(_ => RollRig.Turn(reloaded, 6)).ToList();
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void SavedSeedText_IsAvailableBeforeBuildingTheGame()
        {
            var storage = new FakeSaveStorage();
            var live = RollRig.Build(555UL);
            Service(storage, new DiceSystemSaveContributor(live)).Save("s");

            // Sin ningún sistema vivo: solo se lee para conocer la seed con la que hay que crear la partida.
            Assert.IsTrue(Service(storage).TryRead("s", out var data));
            Assert.AreEqual("555", data.dice.seedText);
            Assert.AreEqual(555UL, GameSeed.FromInput(data.dice.seedText, () => 1UL).Value);
        }

        [Test]
        public void SaveWithoutDiceState_DoesNotBreakApply()
        {
            var sys = RollRig.Build(1UL);
            var service = Service(new FakeSaveStorage(), new DiceSystemSaveContributor(sys));
            service.Apply(new GameSaveData { dice = null });
            Assert.AreEqual(0L, sys.CaptureState().materialsDrawn);
        }

        [Test]
        public void NewerSaveVersion_IsRejected()
        {
            var storage = new FakeSaveStorage();
            storage.Write("future", new FakeSaveSerializer().Serialize(new GameSaveData { version = GameSaveData.CurrentVersion + 1 }));
            Assert.Throws<InvalidOperationException>(() => Service(storage).TryRead("future", out _));
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveService(null, new FakeSaveSerializer(), new FakeSaveStorage()));
            Assert.Throws<ArgumentNullException>(() => Service(new FakeSaveStorage()).Apply(null));
        }
    }
}
