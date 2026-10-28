using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    [CreateAssetMenu(fileName = "TierProportions", menuName = "Dice/Tier Proportion Table")]
    public class TierProportionTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public MaterialTier tier;
            [Min(0)] public int copies;
            public SelectionMode mode; // Bag por defecto (valor 0)
        }

        [SerializeField] private Entry[] entries =
        {
            new Entry { tier = MaterialTier.S, copies = 1,  mode = SelectionMode.Bag },
            new Entry { tier = MaterialTier.A, copies = 4,  mode = SelectionMode.Bag },
            new Entry { tier = MaterialTier.B, copies = 10, mode = SelectionMode.Bag },
            new Entry { tier = MaterialTier.C, copies = 25, mode = SelectionMode.Bag },
        };

        /// <summary>Traduce el SO a datos puros para el Core.</summary>
        public TierRule[] ToRules()
        {
            var rules = new TierRule[entries.Length];
            for (int i = 0; i < entries.Length; i++)
                rules[i] = new TierRule(entries[i].tier, entries[i].copies, entries[i].mode);
            return rules;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<MaterialTier>();
            foreach (var e in entries)
                if (!seen.Add(e.tier))
                    Debug.LogWarning($"[{name}] Tier {e.tier} repetido en la tabla: el roller lanzará excepción.", this);
        }
#endif
    }
}
