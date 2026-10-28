using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class DieMaterialRollerTests
    {
        private static TierRule[] DefaultRules(SelectionMode cMode = SelectionMode.Bag) => new[]
        {
            new TierRule(MaterialTier.S, 1),
            new TierRule(MaterialTier.A, 4),
            new TierRule(MaterialTier.B, 10),
            new TierRule(MaterialTier.C, 25, cMode),
        };

        // 2S, 3A, 4B, 5C
        private static List<IDieMaterial> Pool()
        {
            var list = new List<IDieMaterial>();
            int id = 1;
            foreach (var (tier, n) in new[] { (MaterialTier.S, 2), (MaterialTier.A, 3), (MaterialTier.B, 4), (MaterialTier.C, 5) })
                for (int i = 0; i < n; i++) list.Add(new FakeMaterial(id++, tier));
            return list;
        }

        private static int[] Ids(IEnumerable<IDieMaterial> m) => m.Select(x => x.Id).ToArray();

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new DieMaterialRoller(Pool(), DefaultRules(), 123UL);
            var b = new DieMaterialRoller(Pool(), DefaultRules(), 123UL);
            CollectionAssert.AreEqual(Ids(a.GetRolledDieMaterials(200)), Ids(b.GetRolledDieMaterials(200)));
        }

        [Test]
        public void DifferentSeed_DifferentSequence()
        {
            var a = new DieMaterialRoller(Pool(), DefaultRules(), 1UL);
            var b = new DieMaterialRoller(Pool(), DefaultRules(), 2UL);
            CollectionAssert.AreNotEqual(Ids(a.GetRolledDieMaterials(200)), Ids(b.GetRolledDieMaterials(200)));
        }

        [Test]
        public void GetRolled_EqualsRepeatedGetNext_RegardlessOfGrouping()
        {
            var a = new DieMaterialRoller(Pool(), DefaultRules(), 99UL);
            var b = new DieMaterialRoller(Pool(), DefaultRules(), 99UL);

            var fromBlocks = new List<IDieMaterial>();
            foreach (int n in new[] { 6, 6, 5, 23, 1, 40 }) fromBlocks.AddRange(a.GetRolledDieMaterials(n));

            var fromSingles = new List<IDieMaterial>();
            for (int i = 0; i < fromBlocks.Count; i++) fromSingles.Add(b.GetNextDieMaterial());

            CollectionAssert.AreEqual(Ids(fromBlocks), Ids(fromSingles));
            Assert.AreEqual(a.DrawnCount, b.DrawnCount);
        }

        [Test]
        public void InputOrder_DoesNotAffectSequence()
        {
            var forward = Pool();
            var reversed = Pool(); reversed.Reverse();
            var a = new DieMaterialRoller(forward, DefaultRules(), 5UL);
            var b = new DieMaterialRoller(reversed, DefaultRules(), 5UL);
            CollectionAssert.AreEqual(Ids(a.GetRolledDieMaterials(120)), Ids(b.GetRolledDieMaterials(120)));
        }

        [Test]
        public void EachTierCycle_RespectsProportions_1_4_10_25()
        {
            var roller = new DieMaterialRoller(Pool(), DefaultRules(), 77UL);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var counts = roller.GetRolledDieMaterials(40).GroupBy(m => m.Tier).ToDictionary(g => g.Key, g => g.Count());
                Assert.AreEqual(1, counts[MaterialTier.S]);
                Assert.AreEqual(4, counts[MaterialTier.A]);
                Assert.AreEqual(10, counts[MaterialTier.B]);
                Assert.AreEqual(25, counts[MaterialTier.C]);
            }
        }

        [Test]
        public void BagMode_CommonTier_5Colors_25Draws_EachExactly5()
        {
            var roller = new DieMaterialRoller(Pool(), DefaultRules(), 11UL);
            var perColor = roller.GetRolledDieMaterials(40)
                                 .Where(m => m.Tier == MaterialTier.C)
                                 .GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.Count());
            Assert.AreEqual(5, perColor.Count);
            Assert.That(perColor.Values, Is.All.EqualTo(5));
        }

        [Test]
        public void RandomMode_IsDeterministicWithSameSeed()
        {
            var a = new DieMaterialRoller(Pool(), DefaultRules(SelectionMode.Random), 31UL);
            var b = new DieMaterialRoller(Pool(), DefaultRules(SelectionMode.Random), 31UL);
            CollectionAssert.AreEqual(Ids(a.GetRolledDieMaterials(150)), Ids(b.GetRolledDieMaterials(150)));
        }

        [Test]
        public void RuleWithoutMaterials_IsSkipped_WithWarning()
        {
            var pool = Pool().Where(m => m.Tier != MaterialTier.S).ToList();
            var roller = new DieMaterialRoller(pool, DefaultRules(), 3UL);
            Assert.That(roller.Warnings.Any(w => w.Contains("Tier S")));
            Assert.IsFalse(roller.GetRolledDieMaterials(300).Any(m => m.Tier == MaterialTier.S));
        }

        [Test]
        public void MaterialsWithoutRule_WarnAndNeverAppear()
        {
            var rules = DefaultRules().Where(r => r.Tier != MaterialTier.A).ToArray();
            var roller = new DieMaterialRoller(Pool(), rules, 3UL);
            Assert.That(roller.Warnings.Any(w => w.Contains("Tier A")));
            Assert.IsFalse(roller.GetRolledDieMaterials(300).Any(m => m.Tier == MaterialTier.A));
        }

        [Test]
        public void SingleMaterialInBagMode_Warns()
        {
            var pool = new List<IDieMaterial> { new FakeMaterial(1, MaterialTier.S), new FakeMaterial(2, MaterialTier.C), new FakeMaterial(3, MaterialTier.C) };
            var roller = new DieMaterialRoller(pool, new[] { new TierRule(MaterialTier.S, 1), new TierRule(MaterialTier.C, 4) }, 1UL);
            Assert.That(roller.Warnings.Any(w => w.Contains("un solo material")));
        }

        [Test]
        public void TierBagMultipleOfSix_Warns()
        {
            var pool = new List<IDieMaterial> { new FakeMaterial(1, MaterialTier.S), new FakeMaterial(2, MaterialTier.C) };
            var roller = new DieMaterialRoller(pool, new[] { new TierRule(MaterialTier.S, 2), new TierRule(MaterialTier.C, 4) }, 1UL);
            Assert.That(roller.Warnings.Any(w => w.Contains("múltiplo")));
        }

        [Test]
        public void DuplicateIds_Throw()
        {
            var pool = new List<IDieMaterial> { new FakeMaterial(1, MaterialTier.S), new FakeMaterial(1, MaterialTier.C) };
            Assert.Throws<ArgumentException>(() => new DieMaterialRoller(pool, DefaultRules(), 1UL));
        }

        [Test]
        public void UnassignedId_Throws()
        {
            var pool = new List<IDieMaterial> { new FakeMaterial(0, MaterialTier.C) };
            Assert.Throws<ArgumentException>(() => new DieMaterialRoller(pool, DefaultRules(), 1UL));
        }

        [Test]
        public void DuplicateRuleForTier_Throws()
        {
            var rules = new[] { new TierRule(MaterialTier.C, 1), new TierRule(MaterialTier.C, 2) };
            Assert.Throws<ArgumentException>(() => new DieMaterialRoller(Pool(), rules, 1UL));
        }

        [Test]
        public void NoUsableTier_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new DieMaterialRoller(new List<IDieMaterial>(), DefaultRules(), 1UL));
        }

        [Test]
        public void NegativeQuantity_Throws()
        {
            var roller = new DieMaterialRoller(Pool(), DefaultRules(), 1UL);
            Assert.Throws<ArgumentOutOfRangeException>(() => roller.GetRolledDieMaterials(-1));
        }

        [Test]
        public void FinalScore_RoundsAwayFromZero()
        {
            Assert.AreEqual(8, DieMaterialMath.ComputeFinalScore(5, 1.5f)); // 7.5 -> 8
            Assert.AreEqual(5, DieMaterialMath.ComputeFinalScore(5, 1f));
        }
    }
}
