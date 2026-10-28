using UnityEditor;
using DiceMaterials.Runtime;

namespace DiceMaterials.Editor
{
    [CustomEditor(typeof(DieMaterialSO))]
    public class DieMaterialSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = (DieMaterialSO)target;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField("Id (asignado por la Database)", so.Id);
            DrawDefaultInspector();
        }
    }
}
