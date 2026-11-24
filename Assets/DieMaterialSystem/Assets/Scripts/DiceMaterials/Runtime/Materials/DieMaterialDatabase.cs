using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Registro de materiales + contador de IDs que nunca se reutilizan.
    /// En runtime solo expone una lista de lectura. Asignar/validar/reparar es solo Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "DieMaterialDatabase", menuName = "Dice/Die Material Database")]
    public class DieMaterialDatabase : ScriptableObject
    {
        [SerializeField] private List<DieMaterialSO> materials = new List<DieMaterialSO>();
        [SerializeField] private int nextId = 1; // jamás se calcula como máximo + 1

        public IReadOnlyList<IDieMaterial> Materials => materials; // covarianza de IReadOnlyList
        public int NextId => nextId;

#if UNITY_EDITOR
        public void Editor_Add(DieMaterialSO so) { materials.Add(so); EditorUtility.SetDirty(this); }
        public int Collect() { int n = IdRegistry.Collect(materials); Save(); return n; }
        public int AssignIds() { int n = IdRegistry.AssignIds(materials, ref nextId, this); Save(); return n; }
        public int RepairDuplicates() { int n = IdRegistry.RepairDuplicates(materials, ref nextId); Save(); return n; }
        public List<string> Validate() => IdRegistry.Validate(materials, nextId);

        private void OnValidate()
        {
            foreach (var issue in Validate())
                Debug.LogWarning($"[{name}] {issue}", this);
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
