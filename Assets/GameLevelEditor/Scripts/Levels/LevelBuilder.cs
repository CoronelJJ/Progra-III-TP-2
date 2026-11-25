using System;
using System.Collections.Generic;
using BoardGame.Model;
using DiceMaterials.Runtime;
using UnityEngine;

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

            // 2) Dados precolocados o efecto: cada celda recibe sus propias instancias.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellData data = blueprint.GetCell(x, y);
                    if (!data.playable) continue;

                    Cell cell = board.GetCell(x, y);

                    if (PlaceObstacles(cell, data, x, y)) continue; // los dados mandan sobre el efecto

                    ICellEffect effect = CellData.CloneEffect(data.effect);
                    if (effect != null) cell.SetEffect(effect);
                }
            }

            return board;
        }

        /// <summary>
        /// Carga los dados de la receta directo en la celda (Resolve no valida valores ni límite,
        /// por eso aceptan mezclas que en partida no se permitirían) y recién después le pone el
        /// efecto de bloqueo, para que no se disparen hooks de entrada.
        /// </summary>
        private static bool PlaceObstacles(Cell cell, CellData data, int x, int y)
        {
            if (!data.HasDice) return false;

            if (data.effect != null)
                Debug.LogWarning("LevelBuilder: la celda (" + x + "," + y + ") tiene dados y efecto; se ignora el efecto.");

            var placed = new List<IBoardPiece>();
            for (int i = 0; i < data.dice.Count; i++)
            {
                DiceRecipe recipe = data.dice[i];
                if (recipe == null || recipe.material == null || recipe.face == null)
                {
                    Debug.LogWarning("LevelBuilder: dado " + i + " de la celda (" + x + "," + y + ") incompleto; se omite.");
                    continue;
                }

                var die = new DieData(recipe.material, recipe.face);

                // Cell.Resolve ignora en silencio los tipos que su switch no reconoce.
                // Se verifica que el dado haya entrado de verdad a la cola; si no, no se registra
                // (si no, la celda quedaría bloqueada para siempre por un dado que no existe).
                int before = cell.OccupantCount;
                cell.Resolve(die);
                if (cell.OccupantCount != before + 1)
                {
                    Debug.LogError("LevelBuilder: Cell.Resolve no encoló el dado " + i + " de la celda (" + x + "," + y + "). " +
                                   "Revisa que el 'case' de dados en Cell (Query/Resolve) use el tipo que implementa DieData (IDieData).");
                    continue;
                }

                placed.Add(die);
            }

            if (placed.Count == 0) return false;

            cell.SetEffect(new ObstacleLockCellEffect(placed));
            return true;
        }
    }
}
