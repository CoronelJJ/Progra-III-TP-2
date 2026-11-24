#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Lógica de IDs compartida por las databases (solo Editor). Cada database conserva sus propios campos serializados.</summary>
    internal static class IdRegistry
    {
        public static int Collect<T>(List<T> items) where T : IdentifiedSO
        {
            int added = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var so = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (so != null && !items.Contains(so)) { items.Add(so); added++; }
            }
            return added;
        }

        /// <summary>A cada ítem con id &lt;= 0 le da nextId e incrementa. nextId nunca retrocede.</summary>
        public static int AssignIds<T>(List<T> items, ref int nextId, UnityEngine.Object context) where T : IdentifiedSO
        {
            int max = MaxId(items);
            if (nextId <= max)
            {
                Debug.LogWarning($"[{context.name}] nextId ({nextId}) <= ID existente ({max}). Se corrige a {max + 1}.", context);
                nextId = max + 1;
            }

            int assigned = 0;
            foreach (var so in DistinctNonNull(items))
            {
                if (so.Id > 0) continue;
                so.Editor_SetId(nextId++);
                EditorUtility.SetDirty(so);
                assigned++;
            }
            return assigned;
        }

        /// <summary>Conserva el ID del primero de la lista y reasigna a los demás duplicados.</summary>
        public static int RepairDuplicates<T>(List<T> items, ref int nextId) where T : IdentifiedSO
        {
            int max = MaxId(items);
            if (nextId <= max) nextId = max + 1;

            var seen = new HashSet<int>();
            int repaired = 0;
            foreach (var so in DistinctNonNull(items))
            {
                if (so.Id <= 0) continue;
                if (seen.Add(so.Id)) continue;
                so.Editor_SetId(nextId++);
                EditorUtility.SetDirty(so);
                repaired++;
            }
            return repaired;
        }

        /// <summary>Solo informa (no modifica nada).</summary>
        public static List<string> Validate<T>(List<T> items, int nextId) where T : IdentifiedSO
        {
            var issues = new List<string>();
            var seenSo = new HashSet<T>();
            var idOwner = new Dictionary<int, T>();
            int max = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var so = items[i];
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

        private static int MaxId<T>(List<T> items) where T : IdentifiedSO
        {
            int max = 0;
            foreach (var so in items) if (so != null && so.Id > max) max = so.Id;
            return max;
        }

        private static IEnumerable<T> DistinctNonNull<T>(List<T> items) where T : IdentifiedSO
        {
            var seen = new HashSet<T>();
            foreach (var so in items)
                if (so != null && seen.Add(so)) yield return so;
        }
    }
}
#endif
