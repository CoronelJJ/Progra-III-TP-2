using System.Collections.Generic;
using System.Linq;
using DiceMaterials.Runtime;
using UnityEngine;

namespace BoardGame.Model
{
    /// <summary>
    /// Celda del tablero. Su posición es fija durante toda la partida;
    /// lo único que cambia es su estado (activa/inactiva, ocupación, efecto).
    /// </summary>
    public class Cell
    {
        public readonly Vector2Int Coordinate;

        // Si la celda no es jugable (fuera de la forma del nivel: cruz, rombo, etc.)
        public bool IsActive { get; private set; }

        // Cola FIFO de piezas apiladas. El frente (Peek) es "el más antiguo".
        private readonly Queue<IBoardPiece> _occupancy = new Queue<IBoardPiece>();

        public ICellEffect Effect { get; private set; }

        // Límite base de stacking configurado para la partida (ej: 3).
        private readonly int _baseStackLimit;

        public Cell(Vector2Int coordinate, bool isActive, int baseStackLimit)
        {
            Coordinate = coordinate;
            IsActive = isActive;
            _baseStackLimit = baseStackLimit;
            Effect = new NullCellEffect();
        }

        public void SetActive(bool active) => IsActive = active;

        public void SetEffect(ICellEffect effect)
        {
            // Un efecto bloqueante (obstáculos precolocados) no se puede reemplazar mientras siga activo.
            if (Effect is ILockingCellEffect locking && locking.IsLocked) return;

            Effect = effect ?? new NullCellEffect();
        }

        public bool IsEmpty => _occupancy.Count == 0;

        public int OccupantCount => _occupancy.Count;

        // Valor de frente crudo (sin modificar), el único que lee el evaluador de jugadas.
        public IBoardPiece PeekFront() => _occupancy.Count > 0 ? _occupancy.Peek() : null;

        public IEnumerable<IBoardPiece> AllOccupants() => _occupancy;

        private int EffectiveStackLimit() => Effect.ModifyStackLimit(_baseStackLimit);

        /// <summary>
        /// Consulta si una pieza (dado, modificador de dado o de celda) puede
        /// colocarse acá, sin ejecutar ningún cambio de estado.
        /// </summary>
        public PlacementCheckResult Query(IPlaceableDrop drop, int quantity = 1)
        {
            if (!IsActive)
                return PlacementCheckResult.Rejected(this, RejectionReason.CellInactive);

            switch (drop)
            {
                case IDieData dice:
                    return QueryDice(dice, quantity);

                case IDiceModifierDrop _:
                    if (!Effect.CanAcceptModifier(this))
                        return PlacementCheckResult.Rejected(this, RejectionReason.CellInactive);
                    if (IsEmpty)
                        return PlacementCheckResult.Rejected(this, RejectionReason.NothingToModify);
                    return PlacementCheckResult.Valid(this, PlacementAction.ApplyDiceModifier);

                case ICellModifierDrop _:
                    if (!Effect.CanAcceptModifier(this))
                        return PlacementCheckResult.Rejected(this, RejectionReason.CellInactive);
                    return PlacementCheckResult.Valid(this, PlacementAction.ApplyCellEffect);

                default:
                    return PlacementCheckResult.Rejected(this, RejectionReason.UnknownPieceType);
            }
        }

        private PlacementCheckResult QueryDice(IDieData dice, int quantity)
        {
            if (!Effect.CanAcceptPiece(this, dice))
                return PlacementCheckResult.Rejected(this, RejectionReason.CellInactive);

            int limit = EffectiveStackLimit();

            if (IsEmpty)
            {
                return quantity <= limit
                    ? PlacementCheckResult.Valid(this, PlacementAction.PlaceNew)
                    : PlacementCheckResult.Rejected(this, RejectionReason.StackLimitReached);
            }

            // Si está ocupada, todas las piezas presentes ya comparten el mismo valor,
            // así que alcanza con comparar contra la primera de la cola.
            bool sameValue = _occupancy.Peek().CanStackWith(dice);
            if (!sameValue)
                return PlacementCheckResult.Rejected(this, RejectionReason.OccupiedDifferentValue);

            return (_occupancy.Count + quantity) <= limit
                ? PlacementCheckResult.Valid(this, PlacementAction.StackOnExisting)
                : PlacementCheckResult.Rejected(this, RejectionReason.StackLimitReached);
        }

        /// <summary>
        /// Ejecuta la acción correspondiente sobre esta celda. Se espera que
        /// el llamador ya haya confirmado un resultado válido con Query().
        /// </summary>
        public void Resolve(IPlaceableDrop drop, int quantity = 1)
        {
            switch (drop)
            {
                case IDieData dice:
                    for (int i = 0; i < quantity; i++)
                        EnqueueDice(dice);
                    break;

                case IDiceModifierDrop diceModifier:
                    diceModifier.ApplyTo(_occupancy.OfType<IDieData>().ToList());
                    break;

                case ICellModifierDrop cellModifier:
                    cellModifier.ApplyTo(this);
                    break;
            }
        }

        private void EnqueueDice(IDieData dice)
        {
            _occupancy.Enqueue(dice);
            Effect.OnPieceEnter(this, dice);
        }

        /// <summary>
        /// Saca de la cola la pieza más antigua (usado por la resolución de jugadas).
        /// </summary>
        public IBoardPiece DequeueFront()
        {
            if (_occupancy.Count == 0) return null;

            var piece = _occupancy.Dequeue();
            Effect.OnPieceExit(this, piece);
            return piece;
        }

        /// <summary>
        /// Destruye una pieza puntual sin pasar por el flujo normal de jugada
        /// (usado por ejemplo por el agujero, al tragarse lo que entra).
        /// </summary>
        public void DestroyPiece(IBoardPiece piece)
        {
            // Reconstruye la cola sin la pieza indicada, preservando el orden FIFO.
            var remaining = _occupancy.Where(p => p != piece).ToList();
            _occupancy.Clear();
            foreach (var p in remaining) _occupancy.Enqueue(p);

            Effect.OnPieceExit(this, piece);
        }

        /// <summary>
        /// Vacía completamente la celda (usado por bombas u otros efectos de área).
        /// </summary>
        public void ClearOccupancy()
        {
            while (_occupancy.Count > 0)
            {
                var piece = _occupancy.Dequeue();
                Effect.OnPieceExit(this, piece);
            }
        }

        // Valor de frente ya modificado por el efecto de celda, listo para el evaluador.
        public int? GetEvaluableFrontValue()
        {
            if (PeekFront() is IDieData dice)
                return Effect.ModifyValue(dice.Value);

            return null;
        }
    }
}
