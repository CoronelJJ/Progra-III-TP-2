namespace BoardGame.Model
{
    // Qué acción concreta correspondería ejecutar si la colocación es válida.
    public enum PlacementAction
    {
        None,
        PlaceNew,
        StackOnExisting,
        ApplyDiceModifier,
        ApplyCellEffect
    }

    // Motivo de rechazo, para poder dar feedback específico al jugador (UI, tooltips, etc.).
    public enum RejectionReason
    {
        None,
        CellInactive,
        OutOfBounds,
        OccupiedDifferentValue,
        StackLimitReached,
        NothingToModify,
        UnknownPieceType
    }

    /// <summary>
    /// Veredicto estructurado que devuelve una Cell al ser consultada por una pieza.
    /// El colocador nunca decide reglas de ocupación por su cuenta:
    /// solo lee este resultado y actúa en consecuencia.
    /// </summary>
    public class PlacementCheckResult
    {
        public bool IsValid;
        public PlacementAction Action;
        public RejectionReason Reason;
        public Cell TargetCell;

        public static PlacementCheckResult Rejected(Cell cell, RejectionReason reason)
        {
            return new PlacementCheckResult
            {
                IsValid = false,
                Action = PlacementAction.None,
                Reason = reason,
                TargetCell = cell
            };
        }

        public static PlacementCheckResult Valid(Cell cell, PlacementAction action)
        {
            return new PlacementCheckResult
            {
                IsValid = true,
                Action = action,
                Reason = RejectionReason.None,
                TargetCell = cell
            };
        }
    }
}
