using System;

namespace DiceMaterials
{
    /// <summary>Regla inmutable de un tier: copias en la bolsa de tiers y modo de selección de material.</summary>
    public readonly struct TierRule
    {
        public MaterialTier Tier { get; }
        public int Copies { get; }
        public SelectionMode Mode { get; }

        public TierRule(MaterialTier tier, int copies, SelectionMode mode = SelectionMode.Bag)
        {
            if (copies < 0) throw new ArgumentOutOfRangeException(nameof(copies), "Copies no puede ser negativo.");
            Tier = tier;
            Copies = copies;
            Mode = mode;
        }
    }
}
