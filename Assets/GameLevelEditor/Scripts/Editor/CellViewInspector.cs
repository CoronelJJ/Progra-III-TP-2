using BoardGame.Model;
using BoardGame.View;
using UnityEditor;
using UnityEngine;

namespace BoardGame.Levels.EditorTools
{
    /// <summary>
    /// Muestra en el Inspector (solo lectura) el estado de la Cell del modelo enlazada a este CellView.
    /// La Cell es C# puro, no un componente: esta es la forma de verla en la escena.
    /// </summary>
    [CustomEditor(typeof(CellView))]
    public class CellViewInspector : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var view = (CellView)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Modelo (Cell) · solo lectura", EditorStyles.boldLabel);

            Cell cell = view.Cell;
            if (cell == null)
            {
                EditorGUILayout.HelpBox(
                    "Sin Cell enlazada. La Cell existe solo cuando el BoardView construye el nivel (Play o Build).",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Coordenada", cell.Coordinate.ToString());
            EditorGUILayout.LabelField("Activa", cell.IsActive.ToString());
            EditorGUILayout.LabelField("Efecto", cell.Effect != null ? cell.Effect.GetType().Name : "(ninguno)");
            EditorGUILayout.LabelField("Ocupantes", cell.OccupantCount.ToString());

            int? front = cell.GetEvaluableFrontValue();
            EditorGUILayout.LabelField("Valor del frente", front.HasValue ? front.Value.ToString() : "-");

            var obstacle = cell.Effect as ObstacleLockCellEffect;
            if (obstacle != null)
                EditorGUILayout.LabelField("Obstáculos restantes", obstacle.Remaining.ToString());
        }
    }
}
