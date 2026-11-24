using System;
using System.Globalization;

namespace DiceMaterials
{
    /// <summary>
    /// Seed maestra de la partida + el texto que la representa (para mostrarla, compartirla y guardarla).
    ///
    /// Reglas de conversión texto -> ulong (deterministas y estables entre plataformas):
    ///   - vacío / solo espacios        -> seed ALEATORIA (se genera con la entropía inyectada)
    ///   - solo dígitos y cabe en ulong -> ese número tal cual (una seed aleatoria mostrada como texto vuelve idéntica)
    ///   - cualquier otro texto         -> FNV-1a 64 de los bytes UTF-8 (sensible a mayúsculas) + mezcla SplitMix64
    /// Se hace Trim() del texto. NUNCA se usa string.GetHashCode().
    /// </summary>
    public readonly struct GameSeed
    {
        public ulong Value { get; }
        /// <summary>Texto que el jugador ve/ingresa. Para una seed generada es el número en decimal.</summary>
        public string Text { get; }
        public bool WasGenerated { get; }

        public GameSeed(ulong value, string text, bool wasGenerated)
        {
            Value = value;
            Text = text;
            WasGenerated = wasGenerated;
        }

        public static GameSeed FromInput(string input, Func<ulong> entropy)
        {
            if (string.IsNullOrWhiteSpace(input)) return Generate(entropy);
            return FromText(input);
        }

        /// <summary>Seed a partir de un texto NO vacío (nunca genera una aleatoria).</summary>
        public static GameSeed FromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("El texto de la seed no puede estar vacío.", nameof(text));
            string trimmed = text.Trim();
            return new GameSeed(ParseText(trimmed), trimmed, false);
        }

        public static GameSeed Generate(Func<ulong> entropy)
        {
            if (entropy == null) throw new ArgumentNullException(nameof(entropy));
            ulong value = entropy();
            return new GameSeed(value, value.ToString(CultureInfo.InvariantCulture), true);
        }

        public static ulong ParseText(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                return number;
            return new SplitMix64(StableHash.Fnv1a64(text)).NextULong();
        }
    }
}
