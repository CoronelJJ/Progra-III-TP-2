using System.Collections.Generic;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class SplitMix64Tests
    {
        // Vectores de referencia del algoritmo para seed = 0.
        [Test]
        public void Seed0_MatchesReferenceVectors()
        {
            var rng = new SplitMix64(0UL);
            Assert.AreEqual(0xE220A8397B1DCDAFUL, rng.NextULong());
            Assert.AreEqual(0x6E789E6AA1B965F4UL, rng.NextULong());
            Assert.AreEqual(0x06C45D188009454FUL, rng.NextULong());
        }

        [Test]
        public void NextInt_StaysInRange_AndIsDeterministic()
        {
            var a = new SplitMix64(42);
            var b = new SplitMix64(42);
            for (int i = 0; i < 1000; i++)
            {
                int x = a.NextInt(7);
                Assert.That(x, Is.InRange(0, 6));
                Assert.AreEqual(x, b.NextInt(7));
            }
        }
    }
}
