using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class Fnv1a64Tests
    {
        // Vectores de referencia públicos de FNV-1a 64.
        [Test]
        public void KnownVectors()
        {
            Assert.AreEqual(0xCBF29CE484222325UL, StableHash.Fnv1a64(""));
            Assert.AreEqual(0xAF63DC4C8601EC8CUL, StableHash.Fnv1a64("a"));
        }

        [Test]
        public void Hasher_IsOrderSensitive_AndDeterministic()
        {
            ulong a = new Fnv1a64Hasher().Add(1).Add(2).Value;
            ulong b = new Fnv1a64Hasher().Add(1).Add(2).Value;
            ulong c = new Fnv1a64Hasher().Add(2).Add(1).Value;
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void StringAdd_IsLengthPrefixed_SoConcatenationsDiffer()
        {
            ulong ab_c = new Fnv1a64Hasher().Add("ab").Add("c").Value;
            ulong a_bc = new Fnv1a64Hasher().Add("a").Add("bc").Value;
            Assert.AreNotEqual(ab_c, a_bc);
        }
    }
}
