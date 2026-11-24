using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiceMaterials.Editor
{
    /// <summary>Botones y validación compartidos por los inspectores de las databases.</summary>
    public static class IdDatabaseGUI
    {
        public static void Draw(
            UnityEngine.Object target,
            Func<int> collect, Func<int> assignIds, Func<int> repairDuplicates, Func<List<string>> validate)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Recolectar del proyecto"))
                Debug.Log($"[{target.name}] Agregados: {collect()}", target);

            if (GUILayout.Button("Asignar IDs"))
                Debug.Log($"[{target.name}] IDs asignados: {assignIds()}", target);

            if (GUILayout.Button("Reparar duplicados (conserva el primero de la lista)"))
                Debug.Log($"[{target.name}] IDs reasignados: {repairDuplicates()}", target);

            EditorGUILayout.Space();
            var issues = validate();
            if (issues.Count == 0)
                EditorGUILayout.HelpBox("Sin problemas detectados.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }
    }
}
