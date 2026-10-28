using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class RollerSeedsTests
    {
        [Test]
        public void Derive_IsDeterministic()
        {
            Assert.AreEqual(RollerSeeds.Derive(42UL, RollerSlot.Piece), RollerSeeds.Derive(42UL, RollerSlot.Piece));
        }

        [Test]
        public void Derive_SlotsMapToFixedOutputsOfSplitMix64()
        {
            // Slot N = salida N+1 de SplitMix64(master). Fija el contrato: si cambia, las seeds guardadas se rompen.
            var rng = new SplitMix64(12345UL);
            Assert.AreEqual(rng.NextULong(), RollerSeeds.Derive(12345UL, RollerSlot.Material));
            Assert.AreEqual(rng.NextULong(), RollerSeeds.Derive(12345UL, RollerSlot.DieValue));
            Assert.AreEqual(rng.NextULong(), RollerSeeds.Derive(12345UL, RollerSlot.Piece));
        }

        [Test]
        public void Derive_GivesDifferentSeedPerSlot_AndPerMaster()
        {
            ulong m = RollerSeeds.Derive(1UL, RollerSlot.Material);
            ulong d = RollerSeeds.Derive(1UL, RollerSlot.DieValue);
            ulong p = RollerSeeds.Derive(1UL, RollerSlot.Piece);
            Assert.AreNotEqual(m, d);
            Assert.AreNotEqual(d, p);
            Assert.AreNotEqual(m, p);
            Assert.AreNotEqual(m, RollerSeeds.Derive(2UL, RollerSlot.Material));
        }

        [Test]
        public void SlotValues_AreFrozen()
        {
            Assert.AreEqual(0, (int)RollerSlot.Material);
            Assert.AreEqual(1, (int)RollerSlot.DieValue);
            Assert.AreEqual(2, (int)RollerSlot.Piece);
        }
    }
}
