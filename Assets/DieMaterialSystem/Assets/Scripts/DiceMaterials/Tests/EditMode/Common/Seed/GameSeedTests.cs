using System;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class GameSeedTests
    {
        [Test]
        public void EmptyOrWhitespace_GeneratesRandomSeed_FromEntropy()
        {
            foreach (var input in new[] { null, "", "   " })
            {
                var seed = GameSeed.FromInput(input, () => 98765UL);
                Assert.AreEqual(98765UL, seed.Value);
                Assert.AreEqual("98765", seed.Text);
                Assert.IsTrue(seed.WasGenerated);
            }
        }

        [Test]
        public void NumericText_IsUsedAsIs()
        {
            var seed = GameSeed.FromInput("12345", () => 1UL);
            Assert.AreEqual(12345UL, seed.Value);
            Assert.AreEqual("12345", seed.Text);
            Assert.IsFalse(seed.WasGenerated);
            Assert.AreEqual(ulong.MaxValue, GameSeed.FromInput("18446744073709551615", () => 1UL).Value);
        }

        [Test]
        public void GeneratedSeed_RoundTripsThroughItsText()
        {
            var generated = GameSeed.Generate(() => 18446744073709551000UL);
            var again = GameSeed.FromInput(generated.Text, () => 1UL);
            Assert.AreEqual(generated.Value, again.Value);
        }

        [Test]
        public void Text_IsHashedDeterministically_CaseSensitive_AndTrimmed()
        {
            ulong a = GameSeed.FromInput("hola", () => 1UL).Value;
            Assert.AreEqual(a, GameSeed.FromInput("hola", () => 2UL).Value);
            Assert.AreEqual(a, GameSeed.FromInput("  hola  ", () => 2UL).Value);
            Assert.AreNotEqual(a, GameSeed.FromInput("Hola", () => 1UL).Value);
            Assert.AreEqual("hola", GameSeed.FromInput("  hola  ", () => 1UL).Text);
        }

        [Test]
        public void TextHash_MatchesFnvPlusSplitMix_Contract()
        {
            ulong expected = new SplitMix64(StableHash.Fnv1a64("daily-2026-11-21")).NextULong();
            Assert.AreEqual(expected, GameSeed.ParseText("daily-2026-11-21"));
        }

        [Test]
        public void Overflowing_Number_FallsBackToHash_InsteadOfThrowing()
        {
            ulong v = GameSeed.ParseText("18446744073709551616"); // ulong.Max + 1
            Assert.AreEqual(new SplitMix64(StableHash.Fnv1a64("18446744073709551616")).NextULong(), v);
        }

        [Test]
        public void NullEntropy_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GameSeed.Generate(null));
            Assert.Throws<ArgumentNullException>(() => GameSeed.FromInput("", null));
        }

        [Test]
        public void FromText_NeverGenerates_AndRejectsEmpty()
        {
            var seed = GameSeed.FromText("  777 ");
            Assert.AreEqual(777UL, seed.Value);
            Assert.AreEqual("777", seed.Text);
            Assert.IsFalse(seed.WasGenerated);
            Assert.Throws<ArgumentException>(() => GameSeed.FromText(""));
            Assert.Throws<ArgumentException>(() => GameSeed.FromText("   "));
        }
    }
}
