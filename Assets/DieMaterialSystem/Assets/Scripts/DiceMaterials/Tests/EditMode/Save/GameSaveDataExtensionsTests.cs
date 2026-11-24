using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class GameSaveDataExtensionsTests
    {
        [Test]
        public void TryGetSeed_RecoversTheExactSeed_OfAnyKind()
        {
            foreach (var original in new[]
                     { GameSeed.FromText("12345"), GameSeed.FromText("daily-2026-11-21"), GameSeed.Generate(() => 18446744073709551000UL) })
            {
                var data = new GameSaveData { dice = new DiceSystemState { seedText = original.Text, seedValue = original.Value.ToString() } };

                Assert.IsTrue(data.TryGetSeed(out var recovered));
                Assert.AreEqual(original.Value, recovered.Value);
                Assert.AreEqual(original.Text, recovered.Text);
            }
        }

        [Test]
        public void TryGetSeed_FallsBackToTheText_WhenTheValueIsMissing()
        {
            var data = new GameSaveData { dice = new DiceSystemState { seedText = "hola", seedValue = null } };
            Assert.IsTrue(data.TryGetSeed(out var seed));
            Assert.AreEqual(GameSeed.FromText("hola").Value, seed.Value);
        }

        [Test]
        public void TryGetSeed_ReturnsFalse_WhenThereIsNoSeedInTheSave()
        {
            Assert.IsFalse(new GameSaveData { dice = null }.TryGetSeed(out _));
            Assert.IsFalse(new GameSaveData { dice = new DiceSystemState() }.TryGetSeed(out _));
            Assert.IsFalse(((GameSaveData)null).TryGetSeed(out _));
        }

        [Test]
        public void SeedFromSave_ReproducesTheSameGame()
        {
            var live = RollRig.Build(2026UL);
            RollRig.Turn(live, 6);
            var data = new GameSaveData { dice = live.CaptureState() };

            Assert.IsTrue(data.TryGetSeed(out var seed));
            var reloaded = RollRig.Build(seed.Value);
            reloaded.Restore(data.dice);
            Assert.AreEqual(RollRig.Turn(live, 6), RollRig.Turn(reloaded, 6));
        }
    }
}
