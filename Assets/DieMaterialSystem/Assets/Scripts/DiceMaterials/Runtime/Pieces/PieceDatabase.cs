using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DiceMaterials.Runtime
{
    /// <summary>Registro de piezas + contador de IDs (misma mecánica que DieMaterialDatabase).</summary>
    [CreateAssetMenu(fileName = "PieceDatabase", menuName = "Dice/Piece Database")]
    public class PieceDatabase : ScriptableObject
    {
        [SerializeField] private List<PieceSO> pieces = new List<PieceSO>();
        [SerializeField] private int nextId = 1;

        public IReadOnlyList<IPiece> Pieces => pieces; // covarianza de IReadOnlyList
        public int NextId => nextId;

#if UNITY_EDITOR
        public void Editor_Add(PieceSO so) { pieces.Add(so); EditorUtility.SetDirty(this); }
        public int Collect() { int n = IdRegistry.Collect(pieces); Save(); return n; }
        public int AssignIds() { int n = IdRegistry.AssignIds(pieces, ref nextId, this); Save(); return n; }
        public int RepairDuplicates() { int n = IdRegistry.RepairDuplicates(pieces, ref nextId); Save(); return n; }
        public List<string> Validate() => IdRegistry.Validate(pieces, nextId);

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
