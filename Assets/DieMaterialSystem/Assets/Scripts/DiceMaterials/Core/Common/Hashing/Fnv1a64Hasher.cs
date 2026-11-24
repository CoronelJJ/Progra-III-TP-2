using System.Text;

namespace DiceMaterials
{
    /// <summary>
    /// FNV-1a de 64 bits, estable entre plataformas y versiones (a diferencia de string.GetHashCode()).
    /// Se usa para convertir texto de seed a número y para la huella del contenido.
    /// </summary>
    public sealed class Fnv1a64Hasher
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;
        private ulong hash = Offset;

        public ulong Value => hash;

        public Fnv1a64Hasher AddByte(byte b)
        {
            unchecked { hash ^= b; hash *= Prime; }
            return this;
        }

        /// <summary>8 bytes little-endian.</summary>
        public Fnv1a64Hasher Add(long value)
        {
            for (int i = 0; i < 8; i++) AddByte((byte)(value >> (8 * i)));
            return this;
        }

        public Fnv1a64Hasher Add(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            Add(bytes.Length); // evita ambigüedad entre cadenas concatenadas
            foreach (var b in bytes) AddByte(b);
            return this;
        }
    }

    public static class StableHash
    {
        /// <summary>FNV-1a 64 sobre los bytes UTF-8 del texto (sin prefijo de longitud).</summary>
        public static ulong Fnv1a64(string text)
        {
            var h = new Fnv1a64Hasher();
            foreach (var b in Encoding.UTF8.GetBytes(text ?? string.Empty)) h.AddByte(b);
            return h.Value;
        }
    }
}
