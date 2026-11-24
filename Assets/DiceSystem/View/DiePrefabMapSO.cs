using System;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    [Serializable]
    public struct DiePrefabEntry
    {
        [Min(1)] public int faceCount;
        public Die prefab;
    }

    /// <summary>Tipo de dado (cantidad de caras) → prefab. Lo usa el pool.</summary>
    [CreateAssetMenu(fileName = "DiePrefabMap", menuName = "Dice/Die Prefab Map")]
    public class DiePrefabMapSO : ScriptableObject
    {
        [SerializeField] private DiePrefabEntry[] entries = Array.Empty<DiePrefabEntry>();

        public bool TryGetPrefab(int faceCount, out Die prefab)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].faceCount == faceCount && entries[i].prefab != null)
                {
                    prefab = entries[i].prefab;
                    return true;
                }
            }
            prefab = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].prefab == null)
                    Debug.LogWarning($"[{name}] Entrada {i} sin prefab.", this);
                for (int j = i + 1; j < entries.Length; j++)
                    if (entries[i].faceCount == entries[j].faceCount)
                        Debug.LogWarning($"[{name}] faceCount duplicado: {entries[i].faceCount}.", this);
            }
        }
#endif
    }
}
