using System.Linq;
using NUnit.Framework;
using UnityEngine;
using DiceMaterials.Runtime;

namespace DiceMaterials.Tests
{
    public class IdDatabaseTests
    {
        [Test]
        public void PieceDatabase_AssignIds_IsIncremental_AndNeverReusesIds()
        {
            var db = ScriptableObject.CreateInstance<PieceDatabase>();
            var a = ScriptableObject.CreateInstance<PieceSO>();
            var b = ScriptableObject.CreateInstance<PieceSO>();
            var c = ScriptableObject.CreateInstance<PieceSO>();
            try
            {
                db.Editor_Add(a); db.Editor_Add(b);
                Assert.AreEqual(2, db.AssignIds());
                Assert.AreEqual(1, a.Id);
                Assert.AreEqual(2, b.Id);
                Assert.AreEqual(3, db.NextId);

                db.Editor_Add(c);
                Assert.AreEqual(1, db.AssignIds());
                Assert.AreEqual(3, c.Id);
                Assert.AreEqual(4, db.NextId);
                Assert.AreEqual(0, db.AssignIds()); // no reasigna a los que ya tienen
            }
            finally { foreach (var o in new Object[] { db, a, b, c }) Object.DestroyImmediate(o); }
        }

        [Test]
        public void PieceDatabase_RepairDuplicates_KeepsTheFirst_AndValidateReportsThem()
        {
            var db = ScriptableObject.CreateInstance<PieceDatabase>();
            var a = ScriptableObject.CreateInstance<PieceSO>();
            var b = ScriptableObject.CreateInstance<PieceSO>();
            try
            {
                a.Editor_SetId(5); b.Editor_SetId(5);
                db.Editor_Add(a); db.Editor_Add(b);

                Assert.That(db.Validate().Any(i => i.Contains("duplicado")));
                Assert.AreEqual(1, db.RepairDuplicates());
                Assert.AreEqual(5, a.Id);
                Assert.AreEqual(6, b.Id);
                Assert.AreEqual(0, db.Validate().Count);
            }
            finally { foreach (var o in new Object[] { db, a, b }) Object.DestroyImmediate(o); }
        }

        [Test]
        public void MaterialDatabase_UsesTheSameRules()
        {
            var db = ScriptableObject.CreateInstance<DieMaterialDatabase>();
            var a = ScriptableObject.CreateInstance<DieMaterialSO>();
            var b = ScriptableObject.CreateInstance<DieMaterialSO>();
            try
            {
                db.Editor_Add(a); db.Editor_Add(b);
                Assert.AreEqual(2, db.AssignIds());
                Assert.AreEqual(new[] { 1, 2 }, new[] { a.Id, b.Id });
                Assert.AreEqual(2, db.Materials.Count);
            }
            finally { foreach (var o in new Object[] { db, a, b }) Object.DestroyImmediate(o); }
        }

        [Test]
        public void PieceSO_Defaults()
        {
            var p = ScriptableObject.CreateInstance<PieceSO>();
            try
            {
                Assert.AreEqual(1, p.DiceRequired);
                Assert.AreEqual(0, p.Id);
                Assert.IsTrue(p.Sprite == null);
            }
            finally { Object.DestroyImmediate(p); }
        }
    }
}
