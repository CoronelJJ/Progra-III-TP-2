using BoardGame.View;
using UnityEditor;
using UnityEngine;

namespace BoardGame.Levels.EditorTools
{
    /// <summary>Muestra los datos de la Cell cargada como solo lectura y los mantiene actualizados en Play.</summary>
    [CustomEditor(typeof(CellBehaviour))]
    public class CellBehaviourInspector : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            var behaviour = (CellBehaviour)target;

            if (!behaviour.IsBound)
                EditorGUILayout.HelpBox(
                    "Sin Cell enlazada. Se carga cuando el BoardView construye el nivel (Play o Build).",
                    MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawDefaultInspector();
            }
        }
    }
}
