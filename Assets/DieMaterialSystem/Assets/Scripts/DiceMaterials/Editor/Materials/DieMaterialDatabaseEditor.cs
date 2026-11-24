using UnityEditor;
using DiceMaterials.Runtime;

namespace DiceMaterials.Editor
{
    [CustomEditor(typeof(DieMaterialDatabase))]
    public class DieMaterialDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var db = (DieMaterialDatabase)target;
            DrawDefaultInspector();
            IdDatabaseGUI.Draw(db, db.Collect, db.AssignIds, db.RepairDuplicates, db.Validate);
        }
    }
}
