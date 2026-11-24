using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class DiceRollSystemTests
    {
        private static List<string> Turns(IDiceRollSystem s, params int[] dice)
            => dice.Select(d => RollRig.Turn(s, d)).ToList();

        private static (List<int> ids, List<int> faces) Concat(IEnumerable<DiceRollResult> rolls)
        {
            var list = rolls.ToList();
            return (list.SelectMany(r => r.Materials.Select(m => m.Id)).ToList(), list.SelectMany(r => r.Faces).ToList());
        }

        [Test]
        public void RollDice_ReturnsMatchingLists()
        {
            var r = RollRig.Build(1UL).RollDice(6);
            Assert.AreEqual(6, r.Materials.Count);
            Assert.AreEqual(6, r.Faces.Count);
            foreach (int f in r.Faces) Assert.That(f, Is.InRange(1, 6));
        }

        [Test]
        public void FaceValues_FollowTheConfiguredFaceCount()
        {
            var sys = RollRig.Build(1UL, faceCount: 12);
            for (int i = 0; i < 30; i++)
                foreach (int f in sys.RollDice(6).Faces) Assert.That(f, Is.InRange(1, 12));
        }

        [Test]
        public void AskingOneByOne_GivesTheSameSequenceAsOneBatch()
        {
            var (idsA, facesA) = Concat(new[] { RollRig.Build(808UL).RollDice(6) });

            var singles = RollRig.Build(808UL);
            var (idsB, facesB) = Concat(Enumerable.Range(0, 6).Select(_ => singles.RollDice(1)));

            var mixed = RollRig.Build(808UL);
            var (idsC, facesC) = Concat(new[] { mixed.RollDice(2), mixed.RollDice(3), mixed.RollDice(1) });

            CollectionAssert.AreEqual(idsA, idsB); CollectionAssert.AreEqual(facesA, facesB);
            CollectionAssert.AreEqual(idsA, idsC); CollectionAssert.AreEqual(facesA, facesC);
        }

        [Test]
        public void ZeroDice_ReturnsEmptyLists_AndDoesNotTouchThePieces()
        {
            var sys = RollRig.Build(5UL);
            var r = sys.RollDice(0);
            Assert.AreEqual(0, r.Materials.Count);
            Assert.AreEqual(0, r.Faces.Count);
            var st = sys.CaptureState();
            Assert.AreEqual(0L, st.materialsDrawn);
            Assert.AreEqual(0L, st.piecesDrawn);
        }

        [Test]
        public void NegativeCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RollRig.Build(1UL).RollDice(-1));
        }

        [Test]
        public void DrawPiece_GivesOnePerCall_AndACycleCoversEveryPiece()
        {
            var sys = RollRig.Build(3UL);
            var ids = Enumerable.Range(0, 7).Select(_ => sys.DrawPiece().Id).ToList();
            Assert.AreEqual(7, ids.Distinct().Count());
            Assert.AreEqual(7L, sys.CaptureState().piecesDrawn);
        }

        [Test]
        public void Pieces_AreIndependentFromDice_AndViceVersa()
        {
            // Referencia: las piezas solas.
            var pieceOnly = RollRig.Build(42UL);
            var expectedPieces = Enumerable.Range(0, 20).Select(_ => pieceOnly.DrawPiece().Id).ToList();

            // Mismo sistema con dados normales y extras intercalados.
            var both = RollRig.Build(42UL);
            var gotPieces = new List<int>();
            var rolls = new List<DiceRollResult>();
            for (int i = 0; i < 20; i++)
            {
                rolls.Add(both.RollDice(6));
                if (i % 3 == 0) rolls.Add(both.RollDice(1));
                gotPieces.Add(both.DrawPiece().Id);
            }
            CollectionAssert.AreEqual(expectedPieces, gotPieces);

            // Los dados, juntando todos los lotes, son la secuencia pura de esa cantidad de dados.
            var (gotIds, gotFaces) = Concat(rolls);
            var (expIds, expFaces) = Concat(new[] { RollRig.Build(42UL).RollDice(gotIds.Count) });
            CollectionAssert.AreEqual(expIds, gotIds);
            CollectionAssert.AreEqual(expFaces, gotFaces);
        }

        [Test]
        public void SameSeed_SameTurns()
        {
            var a = RollRig.Build(2024UL);
            var b = RollRig.Build(2024UL);
            CollectionAssert.AreEqual(Turns(a, 6, 6, 3, 6, 4, 6, 6), Turns(b, 6, 6, 3, 6, 4, 6, 6));
        }

        [Test]
        public void DifferentSeed_DifferentTurns()
        {
            CollectionAssert.AreNotEqual(Turns(RollRig.Build(1UL), 6, 6, 6, 6), Turns(RollRig.Build(2UL), 6, 6, 6, 6));
        }

        [Test]
        public void CaptureState_ReportsSeedFingerprintAndCounters()
        {
            var sys = RollRig.Build(77UL);
            for (int i = 0; i < 3; i++) { sys.DrawPiece(); sys.RollDice(6); }
            sys.RollDice(1); // un dado extra
            var st = sys.CaptureState();
            Assert.AreEqual(DiceSystemState.CurrentVersion, st.version);
            Assert.AreEqual("77", st.seedText);
            Assert.AreEqual("77", st.seedValue);
            Assert.AreEqual(sys.Fingerprint.ToString(), st.fingerprint);
            Assert.AreEqual(19L, st.materialsDrawn);
            Assert.AreEqual(19L, st.facesDrawn);
            Assert.AreEqual(3L, st.piecesDrawn);
        }

        [Test]
        public void Restore_ContinuesExactlyWhereTheSavedStateLeftOff()
        {
            var original = RollRig.Build(31415UL);
            Turns(original, 6, 6, 3, 6, 4, 6, 5);   // cruza ciclos de bolsa en manos parciales
            original.RollDice(1);                    // y un dado extra suelto
            var state = original.CaptureState();

            var restored = RollRig.Build(31415UL);
            restored.Restore(state);

            CollectionAssert.AreEqual(Turns(original, 6, 6, 6, 2, 6, 6, 6, 6), Turns(restored, 6, 6, 6, 2, 6, 6, 6, 6));
        }

        [Test]
        public void Restore_AtStart_IsANoOp()
        {
            var fresh = RollRig.Build(9UL);
            fresh.Restore(RollRig.Build(9UL).CaptureState());
            CollectionAssert.AreEqual(Turns(fresh, 6, 6), Turns(RollRig.Build(9UL), 6, 6));
        }

        [Test]
        public void Restore_WithDifferentSeed_Throws()
        {
            var state = RollRig.Build(1UL).CaptureState();
            Assert.Throws<InvalidOperationException>(() => RollRig.Build(2UL).Restore(state));
        }

        [Test]
        public void Restore_WhenContentChanged_Throws()
        {
            var state = RollRig.Build(1UL, faceCount: 6).CaptureState();
            Assert.Throws<InvalidOperationException>(() => RollRig.Build(1UL, faceCount: 12).Restore(state));
            Assert.Throws<InvalidOperationException>(() => RollRig.Build(1UL, copies: 2).Restore(state));
            Assert.Throws<InvalidOperationException>(() => RollRig.Build(1UL, pieceCount: 5).Restore(state));
        }

        [Test]
        public void Restore_UnsupportedVersion_Throws()
        {
            var state = RollRig.Build(1UL).CaptureState();
            state.version = 99;
            Assert.Throws<InvalidOperationException>(() => RollRig.Build(1UL).Restore(state));
        }

        [Test]
        public void Restore_CannotRewind_AndLeavesTheSystemUntouched()
        {
            var advanced = RollRig.Build(4UL);
            Turns(advanced, 6, 6, 6);

            var earlier = RollRig.Build(4UL);
            Turns(earlier, 6);
            var old = earlier.CaptureState();

            var before = advanced.CaptureState();
            Assert.Throws<InvalidOperationException>(() => advanced.Restore(old));
            var after = advanced.CaptureState();
            Assert.AreEqual(before.materialsDrawn, after.materialsDrawn);
            Assert.AreEqual(before.facesDrawn, after.facesDrawn);
            Assert.AreEqual(before.piecesDrawn, after.piecesDrawn);
        }

        [Test]
        public void Restore_InvalidInput_Throws()
        {
            var sys = RollRig.Build(1UL);
            Assert.Throws<ArgumentNullException>(() => sys.Restore(null));
            var state = sys.CaptureState();
            state.piecesDrawn = -1;
            Assert.Throws<InvalidOperationException>(() => sys.Restore(state));
        }
    }
}
