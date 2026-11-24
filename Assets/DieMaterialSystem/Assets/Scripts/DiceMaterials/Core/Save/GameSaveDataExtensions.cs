using System.Globalization;

namespace DiceMaterials
{
    public static class GameSaveDataExtensions
    {
        /// <summary>
        /// Recupera la seed de un guardado para crear la partida con la misma seed (escena de selección, antes de construir el juego).
        /// Usa el valor numérico guardado; si no es válido, vuelve a calcularlo desde el texto.
        /// </summary>
        public static bool TryGetSeed(this GameSaveData data, out GameSeed seed)
        {
            seed = default;
            var dice = data?.dice;
            if (dice == null) return false;

            if (ulong.TryParse(dice.seedValue, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                string text = string.IsNullOrWhiteSpace(dice.seedText)
                    ? value.ToString(CultureInfo.InvariantCulture)
                    : dice.seedText;
                seed = new GameSeed(value, text, false);
                return true;
            }

            if (string.IsNullOrWhiteSpace(dice.seedText)) return false;
            seed = GameSeed.FromText(dice.seedText);
            return true;
        }
    }
}
