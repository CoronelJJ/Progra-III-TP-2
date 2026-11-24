using System.Collections.Generic;
using UnityEngine;

namespace BoardGame.Model
{
    /// <summary>
    /// Matriz fija de celdas para toda la partida. El tamaño y la forma
    /// (qué celdas están activas) se definen al crear el tablero, y no cambian
    /// después: solo cambia el estado interno de cada Cell.
    /// </summary>
    public class Board
    {
        public readonly int Width;
        public readonly int Height;

        private readonly Cell[,] _cells;

        /// <param name="activeMask">
        /// Máscara de forma: true = celda jugable. Si es null, todas las celdas
        /// quedan activas (tablero rectangular completo). Con una máscara distinta
        /// se pueden generar formas como cruz, rombo, letra L, etc. sobre la misma
        /// grilla rectangular base, sin tocar el resto del sistema.
        /// </param>
        public Board(int width, int height, int baseStackLimit, bool[,] activeMask = null)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool active = activeMask == null || activeMask[x, y];
                    _cells[x, y] = new Cell(new Vector2Int(x, y), active, baseStackLimit);
                }
            }
        }

        public bool IsInBounds(Vector2Int coord) =>
            coord.x >= 0 && coord.x < Width && coord.y >= 0 && coord.y < Height;

        public Cell GetCell(Vector2Int coord) => IsInBounds(coord) ? _cells[coord.x, coord.y] : null;

        public Cell GetCell(int x, int y) => GetCell(new Vector2Int(x, y));

        // Devuelve las celdas activas de una fila, en orden de columna.
        public List<Cell> GetRow(int y)
        {
            var row = new List<Cell>();
            for (int x = 0; x < Width; x++)
            {
                var cell = _cells[x, y];
                if (cell.IsActive) row.Add(cell);
            }
            return row;
        }

        // Devuelve las celdas activas de una columna, en orden de fila.
        public List<Cell> GetColumn(int x)
        {
            var column = new List<Cell>();
            for (int y = 0; y < Height; y++)
            {
                var cell = _cells[x, y];
                if (cell.IsActive) column.Add(cell);
            }
            return column;
        }

        public IEnumerable<Cell> AllActiveCells()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_cells[x, y].IsActive)
                        yield return _cells[x, y];
        }
    }
}
