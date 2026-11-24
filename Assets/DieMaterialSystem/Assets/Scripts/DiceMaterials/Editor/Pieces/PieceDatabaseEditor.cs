using UnityEditor;
using DiceMaterials.Runtime;

namespace DiceMaterials.Editor
{
    [CustomEditor(typeof(PieceDatabase))]
    public class PieceDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var db = (PieceDatabase)target;
            DrawDefaultInspector();
            IdDatabaseGUI.Draw(db, db.Collect, db.AssignIds, db.RepairDuplicates, db.Validate);
        }
    }
}
