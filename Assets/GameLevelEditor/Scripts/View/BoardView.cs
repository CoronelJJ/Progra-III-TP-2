using System;
using System.Collections.Generic;
using BoardGame.Levels;
using BoardGame.Model;
using DiceMaterials.Runtime;
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
    /// (modelo puro) con LevelBuilder e instancia un CellView por cada celda ACTIVA y un
    /// BoardDieView por cada dado precolocado. Las celdas inactivas no se dibujan.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Datos")]
        [SerializeField] private LevelBlueprint blueprint;
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool logSummary = true;

        [Header("Dados precolocados (opcional)")]
        [SerializeField] private BoardDieView dieViewPrefab;
        [Range(0.1f, 1f)] [SerializeField] private float dieScale = 0.6f;

        [Header("Layout")]
        [SerializeField] private BoardPlane plane = BoardPlane.XZ;
        [SerializeField] private float cellSize = 1f;
        [Range(0f, 0.5f)] [SerializeField] private float cellGap = 0.05f;

        [Header("Cámara (opcional)")]
        [SerializeField] private Camera fitCamera;
        [SerializeField] private float cameraMargin = 1f;

        [SerializeField, HideInInspector] private Transform container;

        private class LockSubscription
        {
            public ObstacleLockCellEffect Effect;
            public Action<IBoardPiece> Handler;
        }

        private readonly Dictionary<Vector2Int, CellView> _views = new Dictionary<Vector2Int, CellView>();
        private readonly Dictionary<IBoardPiece, BoardDieView> _dieViews = new Dictionary<IBoardPiece, BoardDieView>();
        private readonly List<LockSubscription> _subscriptions = new List<LockSubscription>();
        private bool _warnedMissingDiePrefab;

        public Board Board { get; private set; }

        private void Start()
        {
            if (buildOnStart) Build();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
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
            _warnedMissingDiePrefab = false;
            Board = LevelBuilder.Build(blueprint);

            container = new GameObject("Cells").transform;
            container.SetParent(transform, false);

            // Se escala solo el plano del tablero; el grosor lo define el prefab.
            float size = cellSize * (1f - cellGap);
            Vector3 prefabScale = cellPrefab.transform.localScale;
            float thickness = plane == BoardPlane.XZ ? prefabScale.y : prefabScale.z;
            Vector3 scale = plane == BoardPlane.XZ
                ? new Vector3(size, prefabScale.y, size)
                : new Vector3(size, size, prefabScale.z);

            int activeCells = 0;
            foreach (Cell cell in Board.AllActiveCells())
            {
                activeCells++;
                CellView view = Instantiate(cellPrefab, container);
                view.transform.localPosition = CellToLocal(cell.Coordinate);
                view.transform.localScale = scale;
                view.Bind(cell);
                _views[cell.Coordinate] = view;

                // Componente de celda con la Cell cargada (si el prefab no lo trae, se agrega).
                CellBehaviour behaviour = view.GetComponent<CellBehaviour>();
                if (behaviour == null) behaviour = view.gameObject.AddComponent<CellBehaviour>();
                behaviour.Bind(cell, this);

                SpawnDice(cell, view, thickness);
            }

            FitCamera();

            if (logSummary)
            {
                int loaded = 0;
                foreach (Cell cell in Board.AllActiveCells()) loaded += cell.OccupantCount;

                int expected = blueprint.CountDice();
                string message = "BoardView: " + Board.Width + "x" + Board.Height + ", " + activeCells + " celdas activas. " +
                                 "Dados en el blueprint: " + expected + ", cargados en el Board: " + loaded +
                                 ", vistas de dado creadas: " + _dieViews.Count + ".";
                if (expected != loaded) Debug.LogWarning(message + " (no coinciden: revisa los errores de LevelBuilder)", this);
                else Debug.Log(message, this);
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            UnsubscribeAll();
            _views.Clear();
            _dieViews.Clear();
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

        public CellBehaviour GetCellBehaviour(Vector2Int coord)
        {
            CellView view = GetView(coord);
            return view != null ? view.GetComponent<CellBehaviour>() : null;
        }

        // ------------------------------------------------------------------ dados precolocados

        private void SpawnDice(Cell cell, CellView cellView, float thickness)
        {
            var lockEffect = cell.Effect as ObstacleLockCellEffect;
            if (lockEffect == null) return;

            // Cuando sale un obstáculo: se quita su vista y se repinta la celda (puede quedar libre).
            Action<IBoardPiece> handler = piece => OnObstacleRemoved(piece, cellView);
            lockEffect.ObstacleRemoved += handler;
            _subscriptions.Add(new LockSubscription { Effect = lockEffect, Handler = handler });

            if (dieViewPrefab == null)
            {
                if (!_warnedMissingDiePrefab)
                {
                    Debug.LogWarning("BoardView: hay dados precolocados pero falta asignar el Die View Prefab.", this);
                    _warnedMissingDiePrefab = true;
                }
                return;
            }

            float dieSize = cellSize * dieScale;
            Vector3 axis = plane == BoardPlane.XZ ? Vector3.up : Vector3.back;
            Vector3 basePos = CellToLocal(cell.Coordinate);

            // AllOccupants enumera la cola desde el frente: el primero es el más cercano al tablero.
            int level = 0;
            foreach (IBoardPiece piece in cell.AllOccupants())
            {
                DieData data = piece as DieData;
                if (data == null) continue;

                BoardDieView dieView = Instantiate(dieViewPrefab, container);
                dieView.transform.localPosition = basePos + axis * (thickness * 0.5f + dieSize * (level + 0.5f));
                dieView.transform.localScale = Vector3.one * dieSize;
                dieView.Init(data);
                _dieViews[piece] = dieView;
                level++;
            }
        }

        private void OnObstacleRemoved(IBoardPiece piece, CellView cellView)
        {
            BoardDieView dieView;
            if (_dieViews.TryGetValue(piece, out dieView))
            {
                _dieViews.Remove(piece);
                if (dieView != null)
                {
                    if (Application.isPlaying) Destroy(dieView.gameObject);
                    else DestroyImmediate(dieView.gameObject);
                }
            }

            if (cellView != null) cellView.Refresh();
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Effect.ObstacleRemoved -= _subscriptions[i].Handler;
            _subscriptions.Clear();
        }

        // ------------------------------------------------------------------ coordenadas

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
