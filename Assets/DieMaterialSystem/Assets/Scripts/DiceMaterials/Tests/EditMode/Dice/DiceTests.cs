using BoardGame.Model;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class DiceTests
    {
        [Test]
        public void Constructor_StoresFaceValueAndMaterial()
        {
            var material = new FakeMaterial(1, MaterialTier.S);
            var dice = new Dice(5, material);

            Assert.AreEqual(5, dice.FaceValue);
            Assert.AreEqual(material, dice.CurrentMaterial);
        }

        [Test]
        public void FaceValue_CanBeChanged_MaterialReferenceStaysTheSame()
        {
            var material = new FakeMaterial(1, MaterialTier.C);
            var dice = new Dice(2, material);

            dice.FaceValue = 6;

            Assert.AreEqual(6, dice.FaceValue);
            Assert.AreEqual(material, dice.CurrentMaterial);
        }

        [Test]
        public void ImplementsIDice()
        {
            IDice dice = new Dice(3, new FakeMaterial(2, MaterialTier.B));
            Assert.AreEqual(3, dice.FaceValue);
            Assert.AreEqual(2, dice.CurrentMaterial.Id);
        }
    }
}
