using System.Collections.Generic;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Puente Unity -> Core: traduce el SO de proporciones, deriva la seed del slot Material desde la maestra y loguea los warnings.</summary>
    public static class DieMaterialRollerFactory
    {
        public static DieMaterialRoller Create(IEnumerable<IDieMaterial> pool, TierProportionTable table, ulong masterSeed)
        {
            var roller = new DieMaterialRoller(pool, table.ToRules(), RollerSeeds.Derive(masterSeed, RollerSlot.Material));
            foreach (var w in roller.Warnings)
                Debug.LogWarning($"[DieMaterialRoller] {w}");
            return roller;
        }
    }
}
