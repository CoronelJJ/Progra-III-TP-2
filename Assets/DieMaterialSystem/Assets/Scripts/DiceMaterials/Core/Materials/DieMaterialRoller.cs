using System;
using System.Collections.Generic;

namespace DiceMaterials
{
    /// <summary>
    /// Roller de materiales en dos niveles: bolsa de tiers (copias según TierRule) y,
    /// por tier, un selector de materiales (Bag o Random, 1 copia por material).
    ///
    /// Sub-seeds internas: SplitMix64 las deriva de la seed del roller en orden fijo:
    ///   slot 0 = bolsa de tiers; slots 1..4 = tiers S, A, B, C (un tier vacío igual consume su slot).
    /// Antes de armar las bolsas, los materiales se ordenan por Id.
    /// Recibe la seed YA DERIVADA para este roller (ver RollerSeeds).
    /// Estado reconstruible con (seed, DrawnCount).
    /// </summary>
    public sealed class DieMaterialRoller : IDieMaterialRoller
    {
        /// <summary>Dados por tirada: se avisa si la bolsa de tiers tiene un tamaño múltiplo de esto.</summary>
        public const int DicePerRollToAvoidAsBagMultiple = 6;

        private static readonly MaterialTier[] TierOrder =
            { MaterialTier.S, MaterialTier.A, MaterialTier.B, MaterialTier.C };

        private readonly IPicker<MaterialTier> tierPicker;
        private readonly Dictionary<MaterialTier, IPicker<IDieMaterial>> materialPickers = new();
        private readonly List<string> warnings = new();

        /// <summary>Seed de ESTE roller (derivada de la maestra con RollerSeeds.Derive).</summary>
        public ulong Seed { get; }
        /// <summary>Cantidad de materiales entregados desde la construcción.</summary>
        public long DrawnCount { get; private set; }
        public IReadOnlyList<string> Warnings => warnings;

        public DieMaterialRoller(IEnumerable<IDieMaterial> materials, IReadOnlyList<TierRule> rules, ulong seed)
        {
            if (materials == null) throw new ArgumentNullException(nameof(materials));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            Seed = seed;

            // 1) Orden estable por Id + validación de identidad
            var sorted = new List<IDieMaterial>();
            foreach (var m in materials)
            {
                if (m == null) throw new ArgumentException("La lista de materiales contiene un null.", nameof(materials));
                if (m.Id <= 0) throw new ArgumentException($"El material '{m.Name}' tiene Id {m.Id} (sin asignar).", nameof(materials));
                sorted.Add(m);
            }
            sorted.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 1; i < sorted.Count; i++)
                if (sorted[i].Id == sorted[i - 1].Id)
                    throw new ArgumentException(
                        $"Id duplicado {sorted[i].Id}: '{sorted[i - 1].Name}' y '{sorted[i].Name}'. Rompe el determinismo.",
                        nameof(materials));

            var byTier = new Dictionary<MaterialTier, List<IDieMaterial>>();
            foreach (var m in sorted)
            {
                if (!byTier.TryGetValue(m.Tier, out var list))
                    byTier[m.Tier] = list = new List<IDieMaterial>();
                list.Add(m);
            }

            // 2) Reglas por tier
            var ruleByTier = new Dictionary<MaterialTier, TierRule>();
            foreach (var r in rules)
            {
                if (ruleByTier.ContainsKey(r.Tier))
                    throw new ArgumentException($"Hay más de una regla para el tier {r.Tier}.", nameof(rules));
                ruleByTier[r.Tier] = r;
            }

            // 3) Sub-seeds en orden fijo (siempre se consumen los 5 slots)
            var seeder = new SplitMix64(seed);
            ulong tierBagSeed = seeder.NextULong();
            var tierSeeds = new ulong[TierOrder.Length];
            for (int i = 0; i < tierSeeds.Length; i++) tierSeeds[i] = seeder.NextULong();

            // 4) Selectores de materiales y fichas de la bolsa de tiers
            var tierSlots = new List<MaterialTier>();
            for (int i = 0; i < TierOrder.Length; i++)
            {
                var tier = TierOrder[i];
                bool hasMaterials = byTier.TryGetValue(tier, out var pool);
                bool hasRule = ruleByTier.TryGetValue(tier, out var rule);

                if (hasMaterials && !hasRule)
                {
                    warnings.Add($"Tier {tier}: {pool.Count} material(es) pero no hay regla; nunca saldrán.");
                    continue;
                }
                if (!hasRule) continue;

                if (rule.Copies == 0)
                {
                    if (hasMaterials)
                        warnings.Add($"Tier {tier}: la regla tiene 0 copias; sus {pool.Count} material(es) nunca saldrán.");
                    continue;
                }
                if (!hasMaterials)
                {
                    warnings.Add($"Tier {tier}: la regla tiene {rule.Copies} copias pero no hay materiales; se omite de la bolsa de tiers.");
                    continue;
                }

                if (rule.Mode == SelectionMode.Bag)
                {
                    if (pool.Count == 1)
                        warnings.Add($"Tier {tier}: modo Bag con un solo material; la bolsa no tiene efecto.");
                    materialPickers[tier] = new ShuffleBag<IDieMaterial>(pool, tierSeeds[i]);
                }
                else
                {
                    materialPickers[tier] = new RandomPicker<IDieMaterial>(pool, tierSeeds[i]);
                }

                for (int c = 0; c < rule.Copies; c++) tierSlots.Add(tier);
            }

            if (tierSlots.Count == 0)
                throw new InvalidOperationException("No hay ningún tier con reglas y materiales: no se puede armar la bolsa de tiers.");

            if (tierSlots.Count % DicePerRollToAvoidAsBagMultiple == 0)
                warnings.Add($"La bolsa de tiers tiene {tierSlots.Count} fichas (múltiplo de {DicePerRollToAvoidAsBagMultiple}): patrón más predecible.");

            tierPicker = new ShuffleBag<MaterialTier>(tierSlots, tierBagSeed);
        }

        public IDieMaterial GetNextDieMaterial()
        {
            var tier = tierPicker.Next();
            var material = materialPickers[tier].Next();
            DrawnCount++;
            return material;
        }

        public IReadOnlyList<IDieMaterial> GetRolledDieMaterials(int quantity)
        {
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            var result = new List<IDieMaterial>(quantity);
            for (int i = 0; i < quantity; i++) result.Add(GetNextDieMaterial());
            return result;
        }
    }
}
