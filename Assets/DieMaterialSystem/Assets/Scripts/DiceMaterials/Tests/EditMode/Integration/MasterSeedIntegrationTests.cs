using System.Collections.Generic;
using System.Linq;
using BoardGame.Model;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    /// <summary>Los 3 rollers armados desde una única seed maestra.</summary>
    public class MasterSeedIntegrationTests
    {
        private sealed class Rollers
        {
            public DieMaterialRoller Materials;
            public DieValueRoller Values;
            public PieceRoller Pieces;
        }

        private static Rollers Build(ulong master)
        {
            var materials = new List<IDieMaterial>();
            int id = 1;
            foreach (var (tier, n) in new[] { (MaterialTier.S, 2), (MaterialTier.A, 3), (MaterialTier.B, 4), (MaterialTier.C, 5) })
                for (int i = 0; i < n; i++) materials.Add(new FakeMaterial(id++, tier));

            var rules = new[]
            {
                new TierRule(MaterialTier.S, 1), new TierRule(MaterialTier.A, 4),
                new TierRule(MaterialTier.B, 10), new TierRule(MaterialTier.C, 25),
            };
            var pieces = Enumerable.Range(1, 7).Select(i => (IPiece)new FakePiece(i)).ToList();

            return new Rollers
            {
                Materials = new DieMaterialRoller(materials, rules, RollerSeeds.Derive(master, RollerSlot.Material)),
                Values = new DieValueRoller(6, 4, RollerSeeds.Derive(master, RollerSlot.DieValue)),
                Pieces = new PieceRoller(pieces, RollerSeeds.Derive(master, RollerSlot.Piece)),
            };
        }

        private static int[] MatIds(IEnumerable<IDieMaterial> m) => m.Select(x => x.Id).ToArray();
        private static int[] PieceIds(IEnumerable<IPiece> p) => p.Select(x => x.Id).ToArray();

        [Test]
        public void SameMasterSeed_ReproducesAllThreeRollers()
        {
            var a = Build(2024UL);
            var b = Build(2024UL);
            CollectionAssert.AreEqual(MatIds(a.Materials.GetRolledDieMaterials(120)), MatIds(b.Materials.GetRolledDieMaterials(120)));
            CollectionAssert.AreEqual(a.Values.GetRolledDieValues(120).ToArray(), b.Values.GetRolledDieValues(120).ToArray());
            CollectionAssert.AreEqual(PieceIds(a.Pieces.GetRolledPieces(70)), PieceIds(b.Pieces.GetRolledPieces(70)));
        }

        [Test]
        public void DifferentMasterSeed_ChangesTheRollers()
        {
            var a = Build(1UL);
            var b = Build(2UL);
            CollectionAssert.AreNotEqual(MatIds(a.Materials.GetRolledDieMaterials(120)), MatIds(b.Materials.GetRolledDieMaterials(120)));
            CollectionAssert.AreNotEqual(a.Values.GetRolledDieValues(120).ToArray(), b.Values.GetRolledDieValues(120).ToArray());
            CollectionAssert.AreNotEqual(PieceIds(a.Pieces.GetRolledPieces(70)), PieceIds(b.Pieces.GetRolledPieces(70)));
        }

        [Test]
        public void Rollers_AreIndependent_DrawingFromOneDoesNotAlterTheOthers()
        {
            // Referencia: cada roller consumido por separado.
            var reference = Build(777UL);
            var refMaterials = MatIds(reference.Materials.GetRolledDieMaterials(60));
            var refValues = reference.Values.GetRolledDieValues(60).ToArray();
            var refPieces = PieceIds(reference.Pieces.GetRolledPieces(21));

            // Mismo master, pero intercalando tiradas de los tres, en manos de 6 (como en partida).
            var mixed = Build(777UL);
            var gotMaterials = new List<int>();
            var gotValues = new List<int>();
            var gotPieces = new List<int>();
            for (int turn = 0; turn < 10; turn++)
            {
                gotPieces.AddRange(PieceIds(mixed.Pieces.GetRolledPieces(turn % 3 + 1)));
                gotValues.AddRange(mixed.Values.GetRolledDieValues(6));
                gotMaterials.AddRange(MatIds(mixed.Materials.GetRolledDieMaterials(6)));
            }
            gotPieces.AddRange(PieceIds(mixed.Pieces.GetRolledPieces(21 - gotPieces.Count)));

            CollectionAssert.AreEqual(refMaterials, gotMaterials.ToArray());
            CollectionAssert.AreEqual(refValues, gotValues.ToArray());
            CollectionAssert.AreEqual(refPieces, gotPieces.ToArray());
        }

        [Test]
        public void SixDiceRoll_ValuesAndMaterialsAlwaysSameQuantity()
        {
            var r = Build(5UL);
            for (int turn = 0; turn < 30; turn++)
            {
                Assert.AreEqual(6, r.Values.GetRolledDieValues(6).Count);
                Assert.AreEqual(6, r.Materials.GetRolledDieMaterials(6).Count);
            }
        }

        [Test]
        public void Hand_OfSixValuesAndSixMaterials_BuildsSixDice_Reproducibly()
        {
            List<Dice> Hand(ulong master)
            {
                var r = Build(master);
                var values = r.Values.GetRolledDieValues(6);
                var materials = r.Materials.GetRolledDieMaterials(6);
                return values.Select((v, i) => new Dice(v, materials[i])).ToList();
            }

            var a = Hand(31UL);
            var b = Hand(31UL);

            Assert.AreEqual(6, a.Count);
            foreach (var d in a) Assert.That(d.FaceValue, Is.InRange(1, 6));
            CollectionAssert.AreEqual(a.Select(d => d.FaceValue).ToArray(), b.Select(d => d.FaceValue).ToArray());
            CollectionAssert.AreEqual(a.Select(d => d.CurrentMaterial.Id).ToArray(), b.Select(d => d.CurrentMaterial.Id).ToArray());
        }
    }
}
