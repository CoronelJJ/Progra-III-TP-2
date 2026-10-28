using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceMaterials
{
    /// <summary>Azar uniforme con reposición, con generador propio.</summary>
    public sealed class RandomPicker<T> : IPicker<T>
    {
        private readonly T[] items;
        private readonly SplitMix64 rng;

        public RandomPicker(IEnumerable<T> items, ulong seed)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            this.items = items.ToArray();
            if (this.items.Length == 0) throw new ArgumentException("No puede estar vacío.", nameof(items));
            rng = new SplitMix64(seed);
        }

        public T Next() => items[rng.NextInt(items.Length)];
    }
}
