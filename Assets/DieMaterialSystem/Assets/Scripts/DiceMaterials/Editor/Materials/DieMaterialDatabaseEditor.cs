using UnityEditor;
using UnityEngine;
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

            EditorGUILayout.Space();
            if (GUILayout.Button("Recolectar materiales"))
                Debug.Log($"[{db.name}] Materiales agregados: {db.CollectMaterials()}", db);

            if (GUILayout.Button("Asignar IDs"))
                Debug.Log($"[{db.name}] IDs asignados: {db.AssignIds()}", db);

            if (GUILayout.Button("Reparar duplicados (conserva el primero de la lista)"))
                Debug.Log($"[{db.name}] IDs reasignados: {db.RepairDuplicates()}", db);

            EditorGUILayout.Space();
            var issues = db.Validate();
            if (issues.Count == 0)
                EditorGUILayout.HelpBox("Sin problemas detectados.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }
    }
}
