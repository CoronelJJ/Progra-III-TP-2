using BoardGame.Model;
using UnityEngine;

namespace BoardGame.View
{
    /// <summary>
    /// Componente de cada celda de la escena: tiene cargada la Cell del modelo (con su coordenada,
    /// efecto y ocupación) y es el punto de enganche para la lógica de juego por celda
    /// (clics, soltar piezas, etc.). Funciona junto a CellView, que se encarga solo de lo visual.
    ///
    /// Va en el prefab de celda. Si el prefab no lo tiene, BoardView se lo agrega al instanciar.
    /// </summary>
    [RequireComponent(typeof(CellView))]
    public class CellBehaviour : MonoBehaviour
    {
        [Header("Datos cargados (solo lectura, se actualizan solos)")]
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private bool isActive;
        [SerializeField] private string effectName;
        [SerializeField] private int occupantCount;
        [SerializeField] private int obstaclesRemaining;

        private CellView _view;

        /// <summary>La Cell del modelo (C# puro) enlazada a este objeto.</summary>
        public Cell Cell { get; private set; }

        /// <summary>El BoardView que creó esta celda.</summary>
        public BoardView Owner { get; private set; }

        public bool IsBound => Cell != null;

        public Vector2Int Coordinate => Cell != null ? Cell.Coordinate : coordinate;

        public CellView View
        {
            get
            {
                if (_view == null) _view = GetComponent<CellView>();
                return _view;
            }
        }

        /// <summary>Posición en el mundo del centro de la celda (para ubicar piezas encima).</summary>
        public Vector3 WorldCenter => Owner != null ? Owner.CellToWorld(Coordinate) : transform.position;

        /// <summary>Llamado por BoardView al construir el nivel.</summary>
        public void Bind(Cell cell, BoardView owner)
        {
            Cell = cell;
            Owner = owner;
            SyncData();
        }

        // Atajos a las reglas de la celda: las decide Cell, este componente solo las expone.
        public PlacementCheckResult Query(IPlaceableDrop drop, int quantity = 1) => Cell.Query(drop, quantity);

        public void Resolve(IPlaceableDrop drop, int quantity = 1)
        {
            Cell.Resolve(drop, quantity);
            SyncData();
            View.Refresh();
        }

        /// <summary>Copia el estado de la Cell a los campos visibles en el Inspector.</summary>
        public void SyncData()
        {
            if (Cell == null) return;

            coordinate = Cell.Coordinate;
            isActive = Cell.IsActive;
            effectName = Cell.Effect != null ? Cell.Effect.GetType().Name : "(ninguno)";
            occupantCount = Cell.OccupantCount;

            var obstacle = Cell.Effect as ObstacleLockCellEffect;
            obstaclesRemaining = obstacle != null ? obstacle.Remaining : 0;
        }

#if UNITY_EDITOR
        // Solo en el editor: mantiene el Inspector al día mientras juegas.
        private void Update()
        {
            if (Cell != null) SyncData();
        }
#endif
    }
}
