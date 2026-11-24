using System;
using System.IO;
using NUnit.Framework;
using DiceMaterials.Runtime;

namespace DiceMaterials.Tests
{
    public class SaveInfrastructureTests
    {
        [Test]
        public void JsonUtility_RoundTripsTheWholeSaveData()
        {
            var serializer = new JsonUtilitySaveSerializer();
            var original = new GameSaveData
            {
                dice = new DiceSystemState
                {
                    seedText = "daily-2026-11-21",
                    seedValue = "18446744073709551615",
                    fingerprint = "1234567890123456789",
                    materialsDrawn = 5_000_000_000L,
                    facesDrawn = 42,
                    piecesDrawn = 7,
                }
            };

            var copy = serializer.Deserialize(serializer.Serialize(original));

            Assert.AreEqual(original.version, copy.version);
            Assert.AreEqual(original.dice.seedText, copy.dice.seedText);
            Assert.AreEqual(original.dice.seedValue, copy.dice.seedValue);
            Assert.AreEqual(original.dice.fingerprint, copy.dice.fingerprint);
            Assert.AreEqual(original.dice.materialsDrawn, copy.dice.materialsDrawn);
            Assert.AreEqual(original.dice.facesDrawn, copy.dice.facesDrawn);
            Assert.AreEqual(original.dice.piecesDrawn, copy.dice.piecesDrawn);
        }

        [Test]
        public void FileStorage_WriteReadOverwriteDelete()
        {
            string dir = Path.Combine(Path.GetTempPath(), "dicematerials_tests_" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new FileSaveStorage(dir);
                Assert.IsFalse(storage.Exists("slot_1"));
                Assert.IsFalse(storage.TryRead("slot_1", out _));

                storage.Write("slot_1", "{\"a\":1}");
                Assert.IsTrue(storage.Exists("slot_1"));
                Assert.IsTrue(storage.TryRead("slot_1", out var content));
                Assert.AreEqual("{\"a\":1}", content);

                storage.Write("slot_1", "{\"a\":2}");
                storage.TryRead("slot_1", out content);
                Assert.AreEqual("{\"a\":2}", content);
                Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);

                storage.Delete("slot_1");
                Assert.IsFalse(storage.Exists("slot_1"));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void FileStorage_RejectsUnsafeSlotNames()
        {
            var storage = new FileSaveStorage(Path.GetTempPath());
            foreach (var bad in new[] { "", null, "../x", "a/b", "a\\b", "a.b" })
                Assert.Throws<ArgumentException>(() => storage.Exists(bad));
        }
    }
}
