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
        /// <summary>Agrega a la lista todos los DieMaterialSO del proyecto que aún no estén.</summary>
        public int CollectMaterials()
        {
            int added = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:DieMaterialSO"))
            {
                var so = AssetDatabase.LoadAssetAtPath<DieMaterialSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so != null && !materials.Contains(so)) { materials.Add(so); added++; }
            }
            Save();
            return added;
        }

        /// <summary>A cada material con id &lt;= 0 le da nextId e incrementa.</summary>
        public int AssignIds()
        {
            int max = MaxExistingId();
            if (nextId <= max)
            {
                Debug.LogWarning($"[{name}] nextId ({nextId}) <= ID existente ({max}). Se corrige a {max + 1}.", this);
                nextId = max + 1;
            }

            int assigned = 0;
            foreach (var so in DistinctNonNull())
            {
                if (so.Id > 0) continue;
                so.Editor_SetId(nextId++);
                EditorUtility.SetDirty(so);
                assigned++;
            }
            Save();
            return assigned;
        }

        /// <summary>Conserva el ID del primero de la lista y reasigna a los demás duplicados.</summary>
        public int RepairDuplicates()
        {
            int max = MaxExistingId();
            if (nextId <= max) nextId = max + 1;

            var seen = new HashSet<int>();
            int repaired = 0;
            foreach (var so in DistinctNonNull())
            {
                if (so.Id <= 0) continue;
                if (seen.Add(so.Id)) continue;
                so.Editor_SetId(nextId++);
                EditorUtility.SetDirty(so);
                repaired++;
            }
            Save();
            return repaired;
        }

        /// <summary>Solo informa (no modifica nada).</summary>
        public List<string> Validate()
        {
            var issues = new List<string>();
            var seenSo = new HashSet<DieMaterialSO>();
            var idOwner = new Dictionary<int, DieMaterialSO>();
            int max = 0;

            for (int i = 0; i < materials.Count; i++)
            {
                var so = materials[i];
                if (so == null) { issues.Add($"Entrada nula en la posición {i}."); continue; }
                if (!seenSo.Add(so)) { issues.Add($"'{so.name}' está repetido en la lista."); continue; }
                if (so.Id <= 0) { issues.Add($"'{so.name}' no tiene ID asignado."); continue; }
                if (so.Id > max) max = so.Id;
                if (idOwner.TryGetValue(so.Id, out var other))
                    issues.Add($"ID duplicado {so.Id}: '{other.name}' y '{so.name}'.");
                else idOwner[so.Id] = so;
            }
            if (nextId <= max)
                issues.Add($"nextId ({nextId}) es menor o igual al ID existente {max}.");
            return issues;
        }

        private void OnValidate()
        {
            foreach (var issue in Validate())
                Debug.LogWarning($"[{name}] {issue}", this);
        }

        private int MaxExistingId()
        {
            int max = 0;
            foreach (var so in materials) if (so != null && so.Id > max) max = so.Id;
            return max;
        }

        private IEnumerable<DieMaterialSO> DistinctNonNull()
        {
            var seen = new HashSet<DieMaterialSO>();
            foreach (var so in materials)
                if (so != null && seen.Add(so)) yield return so;
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
