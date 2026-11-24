using System.Collections.Generic;
using BoardGame.Levels;
using BoardGame.Model;
using UnityEngine;

namespace BoardGame.View
{
    /// <summary>Plano del mundo donde se apoya el tablero.</summary>
    public enum BoardPlane
    {
        /// <summary>Tablero "acostado" como suelo: x de la grilla = X del mundo, y de la grilla = Z del mundo.</summary>
        XZ,
        /// <summary>Tablero "parado" de cara a la cámara: x = X, y = Y.</summary>
        XY
    }

    /// <summary>
    /// Único MonoBehaviour "raíz" del tablero. Lee un LevelBlueprint, construye el Board
    /// (modelo puro) con LevelBuilder e instancia un CellView por cada celda ACTIVA.
    /// Las celdas inactivas no se dibujan: así la forma del nivel se ve en pantalla.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Datos")]
        [SerializeField] private LevelBlueprint blueprint;
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private bool buildOnStart = true;

        [Header("Layout")]
        [SerializeField] private BoardPlane plane = BoardPlane.XZ;
        [SerializeField] private float cellSize = 1f;
        [Range(0f, 0.5f)] [SerializeField] private float cellGap = 0.05f;

        [Header("Cámara (opcional)")]
        [SerializeField] private Camera fitCamera;
        [SerializeField] private float cameraMargin = 1f;

        [SerializeField, HideInInspector] private Transform container;

        private readonly Dictionary<Vector2Int, CellView> _views = new Dictionary<Vector2Int, CellView>();

        public Board Board { get; private set; }

        private void Start()
        {
            if (buildOnStart) Build();
        }

        /// <summary>Reconstruye todo el nivel desde el blueprint.</summary>
        [ContextMenu("Build")]
        public void Build()
        {
            if (blueprint == null || cellPrefab == null)
            {
                Debug.LogWarning("BoardView: falta asignar el Blueprint o el Cell Prefab.", this);
                return;
            }

            Clear();
            Board = LevelBuilder.Build(blueprint);

            container = new GameObject("Cells").transform;
            container.SetParent(transform, false);

            // Se escala solo el plano del tablero; el grosor lo define el prefab.
            float size = cellSize * (1f - cellGap);
            Vector3 prefabScale = cellPrefab.transform.localScale;
            Vector3 scale = plane == BoardPlane.XZ
                ? new Vector3(size, prefabScale.y, size)
                : new Vector3(size, size, prefabScale.z);

            foreach (Cell cell in Board.AllActiveCells())
            {
                CellView view = Instantiate(cellPrefab, container);
                view.transform.localPosition = CellToLocal(cell.Coordinate);
                view.transform.localScale = scale;
                view.Bind(cell);
                _views[cell.Coordinate] = view;
            }

            FitCamera();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            _views.Clear();
            Board = null;

            if (container != null)
            {
                if (Application.isPlaying) Destroy(container.gameObject);
                else DestroyImmediate(container.gameObject);
                container = null;
            }
        }

        public CellView GetView(Vector2Int coord)
        {
            CellView view;
            return _views.TryGetValue(coord, out view) ? view : null;
        }

        /// <summary>Posición local (respecto a este objeto) del centro de una celda. El tablero queda centrado.</summary>
        public Vector3 CellToLocal(Vector2Int coord)
        {
            float a = (coord.x - (Board.Width - 1) / 2f) * cellSize;
            float b = (coord.y - (Board.Height - 1) / 2f) * cellSize;
            return plane == BoardPlane.XZ ? new Vector3(a, 0f, b) : new Vector3(a, b, 0f);
        }

        public Vector3 CellToWorld(Vector2Int coord) => transform.TransformPoint(CellToLocal(coord));

        /// <summary>Convierte un punto del mundo a coordenada de celda activa (ignora la altura/profundidad).</summary>
        public bool TryGetCellAtWorld(Vector3 world, out Vector2Int coord)
        {
            coord = default(Vector2Int);
            if (Board == null) return false;

            Vector3 local = transform.InverseTransformPoint(world);
            float a = local.x;
            float b = plane == BoardPlane.XZ ? local.z : local.y;

            int x = Mathf.RoundToInt(a / cellSize + (Board.Width - 1) / 2f);
            int y = Mathf.RoundToInt(b / cellSize + (Board.Height - 1) / 2f);
            coord = new Vector2Int(x, y);

            Cell cell = Board.GetCell(coord);
            return cell != null && cell.IsActive;
        }

        /// <summary>Celda bajo un rayo (intersecta con el plano del tablero). Útil para el mouse.</summary>
        public bool TryGetCellAtRay(Ray ray, out Vector2Int coord)
        {
            coord = default(Vector2Int);
            if (Board == null) return false;

            Vector3 normal = plane == BoardPlane.XZ ? transform.up : transform.forward;
            var boardPlane = new Plane(normal, transform.position);

            float enter;
            if (!boardPlane.Raycast(ray, out enter)) return false;
            return TryGetCellAtWorld(ray.GetPoint(enter), out coord);
        }

        /// <summary>Celda bajo una posición de pantalla (ej: Input.mousePosition).</summary>
        public bool TryGetCellAtScreen(Camera camera, Vector2 screenPosition, out Vector2Int coord)
        {
            coord = default(Vector2Int);
            if (camera == null) return false;
            return TryGetCellAtRay(camera.ScreenPointToRay(screenPosition), out coord);
        }

        /// <summary>
        /// Encuadre aproximado del tablero. Conserva la ROTACIÓN de la cámara (la defines tú en la escena)
        /// y solo ajusta su posición/zoom. Sirve para cámaras ortográficas y de perspectiva.
        /// </summary>
        private void FitCamera()
        {
            if (fitCamera == null || Board == null) return;

            float halfW = Board.Width * cellSize / 2f + cameraMargin;
            float halfH = Board.Height * cellSize / 2f + cameraMargin;
            float radius = Mathf.Sqrt(halfW * halfW + halfH * halfH);

            Transform cam = fitCamera.transform;
            Vector3 center = transform.position;
            Vector3 forward = cam.forward;

            if (fitCamera.orthographic)
            {
                Vector3 normal = plane == BoardPlane.XZ ? transform.up : transform.forward;
                bool topDown = Mathf.Abs(Vector3.Dot(forward, normal)) > 0.99f;

                fitCamera.orthographicSize = topDown
                    ? Mathf.Max(halfH, halfW / fitCamera.aspect)
                    : Mathf.Max(radius, radius / fitCamera.aspect);

                float depth = Mathf.Max(Vector3.Dot(center - cam.position, forward), 10f);
                cam.position = center - forward * depth;
            }
            else
            {
                float vHalf = fitCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * fitCamera.aspect);
                float distance = radius / Mathf.Sin(Mathf.Min(vHalf, hHalf));
                cam.position = center - forward * distance;
            }
        }
    }
}
