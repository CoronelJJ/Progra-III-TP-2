using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceMaterials
{
    /// <summary>
    /// Huella estable de TODO lo que influye en las secuencias: si cambia, una seed guardada ya no
    /// reproduce la misma partida. No depende del orden de entrada. Familia, puntaje y nombres no cuentan.
    /// </summary>
    public static class ContentFingerprint
    {
        private const long SchemaVersion = 1;

        public static ulong Compute(
            IEnumerable<IDieMaterial> materials,
            IReadOnlyList<TierRule> rules,
            int faceCount,
            int copiesPerFace,
            SelectionMode faceMode,
            IEnumerable<IPiece> pieces)
        {
            if (materials == null) throw new ArgumentNullException(nameof(materials));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (pieces == null) throw new ArgumentNullException(nameof(pieces));

            var h = new Fnv1a64Hasher();
            h.Add(SchemaVersion);

            var mats = materials.OrderBy(m => m.Id).ToList();
            h.Add(mats.Count);
            foreach (var m in mats) { h.Add(m.Id); h.Add((int)m.Tier); }

            var orderedRules = rules.OrderBy(r => (int)r.Tier).ToList();
            h.Add(orderedRules.Count);
            foreach (var r in orderedRules) { h.Add((int)r.Tier); h.Add(r.Copies); h.Add((int)r.Mode); }

            h.Add(faceCount);
            h.Add(copiesPerFace);
            h.Add((int)faceMode);

            var ps = pieces.OrderBy(p => p.Id).ToList();
            h.Add(ps.Count);
            foreach (var p in ps) h.Add(p.Id);

            return h.Value;
        }
    }
}
