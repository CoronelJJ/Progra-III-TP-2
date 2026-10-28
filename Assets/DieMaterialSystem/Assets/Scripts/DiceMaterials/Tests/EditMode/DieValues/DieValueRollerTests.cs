using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class DieValueRollerTests
    {
        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new DieValueRoller(6, 4, 100UL);
            var b = new DieValueRoller(6, 4, 100UL);
            CollectionAssert.AreEqual(a.GetRolledDieValues(200).ToArray(), b.GetRolledDieValues(200).ToArray());
        }

        [Test]
        public void DifferentSeed_DifferentSequence()
        {
            var a = new DieValueRoller(6, 4, 1UL);
            var b = new DieValueRoller(6, 4, 2UL);
            CollectionAssert.AreNotEqual(a.GetRolledDieValues(200).ToArray(), b.GetRolledDieValues(200).ToArray());
        }

        [Test]
        public void Values_AreWithin_1_And_MaxFace()
        {
            foreach (var mode in new[] { SelectionMode.Bag, SelectionMode.Random })
            {
                var roller = new DieValueRoller(8, 3, 5UL, mode);
                foreach (int v in roller.GetRolledDieValues(500))
                    Assert.That(v, Is.InRange(1, 8));
            }
        }

        [Test]
        public void BagMode_EachCycle_HasEveryFaceExactlyCopiesTimes()
        {
            var roller = new DieValueRoller(6, 4, 9UL); // bolsa de 24
            for (int cycle = 0; cycle < 5; cycle++)
            {
                var counts = roller.GetRolledDieValues(24).GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
                Assert.AreEqual(6, counts.Count);
                Assert.That(counts.Values, Is.All.EqualTo(4));
            }
        }

        [Test]
        public void BagMode_CycleCrossesHandBoundaries()
        {
            // Bolsa de 4 (2 caras x 2 copias); manos de 3: el ciclo se completa a mitad de la 2ª mano.
            var roller = new DieValueRoller(2, 2, 77UL);
            var all = new List<int>();
            all.AddRange(roller.GetRolledDieValues(3));
            all.AddRange(roller.GetRolledDieValues(3));
            all.AddRange(roller.GetRolledDieValues(2));

            Assert.AreEqual(2, all.Take(4).Count(v => v == 1));
            Assert.AreEqual(2, all.Take(4).Count(v => v == 2));
            Assert.AreEqual(2, all.Skip(4).Count(v => v == 1));
            Assert.AreEqual(2, all.Skip(4).Count(v => v == 2));
        }

        [Test]
        public void GetRolled_EqualsRepeatedGetNext_RegardlessOfGrouping()
        {
            var a = new DieValueRoller(6, 4, 55UL);
            var b = new DieValueRoller(6, 4, 55UL);

            var fromBlocks = new List<int>();
            foreach (int n in new[] { 6, 6, 5, 23, 1, 40 }) fromBlocks.AddRange(a.GetRolledDieValues(n));

            var fromSingles = new List<int>();
            for (int i = 0; i < fromBlocks.Count; i++) fromSingles.Add(b.GetNextDieValue());

            CollectionAssert.AreEqual(fromBlocks.ToArray(), fromSingles.ToArray());
            Assert.AreEqual(a.DrawnCount, b.DrawnCount);
            Assert.AreEqual(81L, a.DrawnCount);
        }

        [Test]
        public void RequestedQuantity_IsAlwaysReturned()
        {
            var roller = new DieValueRoller(6, 1, 3UL); // bolsa de 6, se piden 6 muchas veces
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(6, roller.GetRolledDieValues(6).Count);
            Assert.AreEqual(0, roller.GetRolledDieValues(0).Count);
            Assert.AreEqual(13, roller.GetRolledDieValues(13).Count); // mayor que la bolsa
        }

        [Test]
        public void RandomMode_IsDeterministicWithSameSeed()
        {
            var a = new DieValueRoller(6, 1, 31UL, SelectionMode.Random);
            var b = new DieValueRoller(6, 1, 31UL, SelectionMode.Random);
            CollectionAssert.AreEqual(a.GetRolledDieValues(150).ToArray(), b.GetRolledDieValues(150).ToArray());
        }

        [Test]
        public void MaxFaceOne_AlwaysReturnsOne()
        {
            var roller = new DieValueRoller(1, 1, 4UL);
            Assert.That(roller.GetRolledDieValues(20), Is.All.EqualTo(1));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DieValueRoller(0, 1, 1UL));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DieValueRoller(6, 0, 1UL));
            var roller = new DieValueRoller(6, 1, 1UL);
            Assert.Throws<ArgumentOutOfRangeException>(() => roller.GetRolledDieValues(-1));
        }
    }
}
