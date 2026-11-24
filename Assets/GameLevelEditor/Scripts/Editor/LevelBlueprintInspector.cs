using UnityEditor;
using UnityEngine;

namespace BoardGame.Levels.EditorTools
{
    /// <summary>
    /// Inspector simple del LevelBlueprint: muestra un resumen y un botón para abrir el editor de niveles.
    /// </summary>
    [CustomEditor(typeof(LevelBlueprint))]
    public class LevelBlueprintInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var blueprint = (LevelBlueprint)target;

            EditorGUILayout.LabelField("Tamaño", blueprint.Width + " x " + blueprint.Height);
            EditorGUILayout.LabelField("Celdas jugables", blueprint.CountPlayable().ToString());

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseStackLimit"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("Abrir en el editor de niveles", GUILayout.Height(28)))
                LevelBlueprintEditorWindow.Open(blueprint);
        }
    }
}
