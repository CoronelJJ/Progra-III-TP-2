using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class PieceRollerTests
    {
        private static List<IPiece> SevenPieces()
            => Enumerable.Range(1, 7).Select(i => (IPiece)new FakePiece(i)).ToList();

        private static int[] Ids(IEnumerable<IPiece> p) => p.Select(x => x.Id).ToArray();

        [Test]
        public void EachCycle_HasEveryPieceExactlyOnce()
        {
            var roller = new PieceRoller(SevenPieces(), 8UL);
            for (int cycle = 0; cycle < 10; cycle++)
                Assert.AreEqual(7, roller.GetRolledPieces(7).Select(p => p.Id).Distinct().Count());
        }

        [Test]
        public void Cycle_HoldsEvenWhenPulledInHandsThatDontAlign()
        {
            var roller = new PieceRoller(SevenPieces(), 21UL);
            var all = new List<IPiece>();
            for (int i = 0; i < 7; i++) all.AddRange(roller.GetRolledPieces(3)); // 21 = 3 ciclos
            for (int cycle = 0; cycle < 3; cycle++)
                Assert.AreEqual(7, all.Skip(cycle * 7).Take(7).Select(p => p.Id).Distinct().Count());
        }

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new PieceRoller(SevenPieces(), 5UL);
            var b = new PieceRoller(SevenPieces(), 5UL);
            CollectionAssert.AreEqual(Ids(a.GetRolledPieces(100)), Ids(b.GetRolledPieces(100)));
        }

        [Test]
        public void DifferentSeed_DifferentSequence()
        {
            var a = new PieceRoller(SevenPieces(), 1UL);
            var b = new PieceRoller(SevenPieces(), 2UL);
            CollectionAssert.AreNotEqual(Ids(a.GetRolledPieces(100)), Ids(b.GetRolledPieces(100)));
        }

        [Test]
        public void InputOrder_DoesNotAffectSequence()
        {
            var reversed = SevenPieces(); reversed.Reverse();
            var a = new PieceRoller(SevenPieces(), 13UL);
            var b = new PieceRoller(reversed, 13UL);
            CollectionAssert.AreEqual(Ids(a.GetRolledPieces(70)), Ids(b.GetRolledPieces(70)));
        }

        [Test]
        public void GetRolled_EqualsRepeatedGetNext_RegardlessOfGrouping()
        {
            var a = new PieceRoller(SevenPieces(), 99UL);
            var b = new PieceRoller(SevenPieces(), 99UL);

            var fromBlocks = new List<IPiece>();
            foreach (int n in new[] { 4, 4, 3, 17, 1, 20 }) fromBlocks.AddRange(a.GetRolledPieces(n));

            var fromSingles = new List<IPiece>();
            for (int i = 0; i < fromBlocks.Count; i++) fromSingles.Add(b.GetNextPiece());

            CollectionAssert.AreEqual(Ids(fromBlocks), Ids(fromSingles));
            Assert.AreEqual(a.DrawnCount, b.DrawnCount);
            Assert.AreEqual(49L, a.DrawnCount);
        }

        [Test]
        public void SinglePiece_AlwaysThatPiece()
        {
            var roller = new PieceRoller(new IPiece[] { new FakePiece(9) }, 1UL);
            Assert.That(Ids(roller.GetRolledPieces(5)), Is.All.EqualTo(9));
        }

        [Test]
        public void InvalidInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PieceRoller(null, 1UL));
            Assert.Throws<ArgumentException>(() => new PieceRoller(new List<IPiece>(), 1UL));
            Assert.Throws<ArgumentException>(() => new PieceRoller(new IPiece[] { new FakePiece(1), new FakePiece(1) }, 1UL));
            Assert.Throws<ArgumentException>(() => new PieceRoller(new IPiece[] { new FakePiece(1), null }, 1UL));
            var roller = new PieceRoller(SevenPieces(), 1UL);
            Assert.Throws<ArgumentOutOfRangeException>(() => roller.GetRolledPieces(-1));
        }
    }
}
