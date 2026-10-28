using System;

namespace DiceMaterials
{
    /// <summary>
    /// SplitMix64. Sirve tanto de generador como de derivador de sub-seeds.
    /// Versión mínima: se unificará en el chat de seeds.
    /// </summary>
    public sealed class SplitMix64
    {
        private ulong state;

        public SplitMix64(ulong seed) { state = seed; }

        public ulong NextULong()
        {
            unchecked
            {
                ulong z = (state += 0x9E3779B97F4A7C15UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Entero uniforme en [0, bound) sin sesgo de módulo (rechazo).</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
            ulong b = (ulong)bound;
            ulong threshold = unchecked(0UL - b) % b; // 2^64 mod b
            while (true)
            {
                ulong r = NextULong();
                if (r >= threshold) return (int)(r % b);
            }
        }
    }
}
