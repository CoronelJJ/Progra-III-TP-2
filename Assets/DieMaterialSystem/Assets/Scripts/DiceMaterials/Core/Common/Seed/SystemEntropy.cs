using System;
using System.Security.Cryptography;

namespace DiceMaterials
{
    /// <summary>Fuente de entropía del sistema para seeds aleatorias. Inyectable como Func&lt;ulong&gt; en los tests.</summary>
    public static class SystemEntropy
    {
        public static ulong NextUInt64()
        {
            var bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
