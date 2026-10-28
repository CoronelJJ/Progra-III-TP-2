using System;

namespace DiceMaterials
{
    /// <summary>
    /// Deriva la seed de cada roller a partir de la seed maestra de la partida.
    /// Es una función pura: Derive(master, slot) siempre da lo mismo, sin importar
    /// qué otros rollers existan o cuántas veces se haya llamado antes.
    /// La seed derivada es una SALIDA de SplitMix64 (no un estado), por lo que los
    /// flujos de distintos rollers no quedan desplazados entre sí.
    /// </summary>
    public static class RollerSeeds
    {
        public static ulong Derive(ulong masterSeed, RollerSlot slot)
        {
            int index = (int)slot;
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(slot));

            var rng = new SplitMix64(masterSeed);
            ulong value = 0;
            for (int i = 0; i <= index; i++) value = rng.NextULong();
            return value;
        }
    }
}
