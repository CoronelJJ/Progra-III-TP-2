using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceMaterials
{
    /// <summary>
    /// Bolsa: mezcla todo (Fisher-Yates) y avanza un cursor. El azar solo se consume al mezclar.
    /// Al agotarse se vuelve a mezclar. Sin guard de borde ni refill anticipado (a propósito).
    /// Los elementos se pasan ya expandidos (si hay copias, repetidos).
    /// </summary>
    public sealed class ShuffleBag<T> : IPicker<T>
    {
        private readonly T[] items;
        private readonly SplitMix64 rng;
        private int cursor;

        public int Size => items.Length;
        public int Remaining => items.Length - cursor;

        public ShuffleBag(IEnumerable<T> items, ulong seed)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            this.items = items.ToArray();
            if (this.items.Length == 0) throw new ArgumentException("La bolsa no puede estar vacía.", nameof(items));
            rng = new SplitMix64(seed);
            cursor = this.items.Length; // fuerza mezcla en el primer Next()
        }

        public T Next()
        {
            if (cursor >= items.Length)
            {
                Shuffle();
                cursor = 0;
            }
            return items[cursor++];
        }

        private void Shuffle()
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
