using System.Linq;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class ContentFingerprintTests
    {
        private static ulong Fp(
            System.Collections.Generic.IEnumerable<IDieMaterial> mats = null, TierRule[] rules = null,
            int faces = 6, int copies = 4, SelectionMode mode = SelectionMode.Bag,
            System.Collections.Generic.IEnumerable<IPiece> pieces = null)
            => ContentFingerprint.Compute(mats ?? RollRig.Materials(), rules ?? RollRig.Rules(), faces, copies, mode, pieces ?? RollRig.Pieces());

        [Test]
        public void SameContent_SameFingerprint_RegardlessOfInputOrder()
        {
            Assert.AreEqual(Fp(), Fp());
            Assert.AreEqual(Fp(), Fp(mats: RollRig.Materials().AsEnumerable().Reverse(), pieces: RollRig.Pieces().AsEnumerable().Reverse(),
                                     rules: RollRig.Rules().Reverse().ToArray()));
        }

        [Test]
        public void AnythingThatAffectsSequences_ChangesTheFingerprint()
        {
            ulong baseline = Fp();

            var moved = RollRig.Materials(); moved[0] = new FakeMaterial(1, MaterialTier.A);               // cambia un tier
            Assert.AreNotEqual(baseline, Fp(mats: moved));

            var extra = RollRig.Materials(); extra.Add(new FakeMaterial(99, MaterialTier.C));               // agrega un material
            Assert.AreNotEqual(baseline, Fp(mats: extra));

            var rules = RollRig.Rules(); rules[0] = new TierRule(MaterialTier.S, 2);                       // copias
            Assert.AreNotEqual(baseline, Fp(rules: rules));

            var mode = RollRig.Rules(); mode[3] = new TierRule(MaterialTier.C, 25, SelectionMode.Random);  // modo
            Assert.AreNotEqual(baseline, Fp(rules: mode));

            Assert.AreNotEqual(baseline, Fp(faces: 12));
            Assert.AreNotEqual(baseline, Fp(copies: 2));
            Assert.AreNotEqual(baseline, Fp(mode: SelectionMode.Random));
            Assert.AreNotEqual(baseline, Fp(pieces: RollRig.Pieces(5)));
        }

        [Test]
        public void ScoreFamilyAndName_DoNotChangeTheFingerprint()
        {
            var mats = RollRig.Materials();
            mats[0] = new FakeMaterial(1, MaterialTier.S, MaterialFamily.Gem, score: 99, multiplier: 3f);
            Assert.AreEqual(Fp(), Fp(mats: mats));
        }
    }
}
