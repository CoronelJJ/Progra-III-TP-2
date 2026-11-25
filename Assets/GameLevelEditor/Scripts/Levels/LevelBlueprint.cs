using UnityEngine;

namespace BoardGame.Levels
{
    /// <summary>
    /// Plantilla de un nivel: tamaño de la grilla, límite base de stacking y los datos
    /// de cada celda. Es solo DATOS; para obtener un Board jugable usar LevelBuilder.Build().
    /// Unity no serializa matrices 2D, por eso las celdas van en un array plano
    /// con índice = y * width + x.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "BoardGame/Level Blueprint")]
    public class LevelBlueprint : ScriptableObject
    {
        public const int DefaultSize = 8;

        [SerializeField, HideInInspector] private int width = DefaultSize;
        [SerializeField, HideInInspector] private int height = DefaultSize;
        [SerializeField, HideInInspector] private CellData[] cells;
        [SerializeField, Min(1)] private int baseStackLimit = 3;

        public int Width => width;
        public int Height => height;

        public int BaseStackLimit
        {
            get => baseStackLimit;
            set => baseStackLimit = Mathf.Max(1, value);
        }

        private void OnEnable() => EnsureInitialized();

        private void OnValidate() => EnsureInitialized();

        public bool IsInBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

        public int ToIndex(int x, int y) => y * width + x;

        /// <summary>Devuelve los datos de una celda, o null si está fuera de rango.</summary>
        public CellData GetCell(int x, int y)
        {
            if (cells == null || !IsInBounds(x, y)) return null;
            return cells[ToIndex(x, y)];
        }

        /// <summary>Deja el blueprint con el tamaño dado y todas las celdas jugables y sin efecto.</summary>
        public void Initialize(int newWidth, int newHeight)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            cells = new CellData[width * height];
            for (int i = 0; i < cells.Length; i++) cells[i] = new CellData();
        }

        /// <summary>
        /// Cambia el tamaño conservando las celdas existentes (por coordenada).
        /// Las celdas nuevas quedan jugables y sin efecto; las que sobran se descartan.
        /// </summary>
        public void Resize(int newWidth, int newHeight)
        {
            EnsureInitialized();
            newWidth = Mathf.Max(1, newWidth);
            newHeight = Mathf.Max(1, newHeight);

            var resized = new CellData[newWidth * newHeight];
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    bool existed = x < width && y < height;
                    resized[y * newWidth + x] = existed ? cells[y * width + x] : new CellData();
                }
            }

            width = newWidth;
            height = newHeight;
            cells = resized;
        }

        /// <summary>Repara el array si es null, tiene un largo inválido o contiene nulls.</summary>
        public void EnsureInitialized()
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;

            int expected = width * height;
            if (cells == null || cells.Length != expected)
            {
                var old = cells;
                cells = new CellData[expected];
                if (old != null) System.Array.Copy(old, cells, Mathf.Min(old.Length, expected));
            }

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) cells[i] = new CellData();
                if (cells[i].dice == null) cells[i].dice = new System.Collections.Generic.List<DiceRecipe>();
            }
        }

        /// <summary>Total de dados precolocados en el nivel (se calcula, no se guarda).</summary>
        public int CountDice()
        {
            if (cells == null) return 0;
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] != null && cells[i].HasDice) count += cells[i].dice.Count;
            return count;
        }

        public int CountCellsWithDice()
        {
            if (cells == null) return 0;
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] != null && cells[i].HasDice) count++;
            return count;
        }

        public int CountPlayable()
        {
            if (cells == null) return 0;
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] != null && cells[i].playable) count++;
            return count;
        }
    }
}
