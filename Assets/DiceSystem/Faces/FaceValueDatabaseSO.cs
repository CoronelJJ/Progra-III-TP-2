using System.Collections.Generic;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Los FaceValueSO de 1 a 12, serializados aquí. Se resuelve por Value (único).</summary>
    [CreateAssetMenu(fileName = "FaceValueDatabase", menuName = "Dice/Face Value Database")]
    public class FaceValueDatabaseSO : ScriptableObject, IFaceValueProvider
    {
        [SerializeField] private FaceValueSO[] faces = System.Array.Empty<FaceValueSO>();

        private Dictionary<int, FaceValueSO> lookup;

        public bool TryGet(int value, out FaceValueSO face)
        {
            EnsureLookup();
            return lookup.TryGetValue(value, out face);
        }

        private void EnsureLookup()
        {
            if (lookup != null) return;

            var map = new Dictionary<int, FaceValueSO>(faces.Length);
            for (int i = 0; i < faces.Length; i++)
            {
                var f = faces[i];
                if (f == null)
                    throw new System.InvalidOperationException($"[{name}] La entrada {i} es null.");
                if (!map.TryAdd(f.Value, f))
                    throw new System.InvalidOperationException(
                        $"[{name}] Valor duplicado {f.Value}: '{map[f.Value].name}' y '{f.name}'.");
            }
            lookup = map;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lookup = null; // se reconstruye con los cambios del inspector
            var seen = new HashSet<int>();
            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i] == null) { Debug.LogWarning($"[{name}] Entrada {i} vacía.", this); continue; }
                if (!seen.Add(faces[i].Value))
                    Debug.LogWarning($"[{name}] Valor duplicado: {faces[i].Value}.", this);
            }
        }
#endif
    }
}
