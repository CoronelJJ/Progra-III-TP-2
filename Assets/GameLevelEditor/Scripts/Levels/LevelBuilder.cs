using System;
using BoardGame.Model;

namespace BoardGame.Levels
{
    /// <summary>
    /// Convierte un LevelBlueprint (datos) en un Board (modelo de juego).
    /// "Recrear el nivel" es simplemente volver a llamar a Build().
    /// No toca la escena: solo crea objetos del modelo.
    /// </summary>
    public static class LevelBuilder
    {
        public static Board Build(LevelBlueprint blueprint)
        {
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));

            blueprint.EnsureInitialized();
            int width = blueprint.Width;
            int height = blueprint.Height;

            // 1) Máscara de forma: qué celdas son jugables.
            var mask = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    mask[x, y] = blueprint.GetCell(x, y).playable;

            var board = new Board(width, height, blueprint.BaseStackLimit, mask);

            // 2) Efectos: cada celda recibe su propia copia.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellData data = blueprint.GetCell(x, y);
                    if (!data.playable) continue;

                    ICellEffect effect = CellData.CloneEffect(data.effect);
                    if (effect != null) board.GetCell(x, y).SetEffect(effect);
                }
            }

            return board;
        }
    }
}
