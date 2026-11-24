using UnityEditor;
using DiceMaterials.Runtime;

namespace DiceMaterials.Editor
{
    /// <summary>Muestra el Id (solo lectura) en todos los assets con identidad: materiales y piezas.</summary>
    [CustomEditor(typeof(IdentifiedSO), true)]
    public class IdentifiedSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = (IdentifiedSO)target;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField("Id (asignado por la Database)", so.Id);
            DrawDefaultInspector();
        }
    }
}
