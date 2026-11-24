namespace BoardGame.Model
{
    /// <summary>
    /// Pieza especial que, al caer sobre una celda, reemplaza (o remueve) el
    /// ICellEffect asignado a esa celda puntual. No ocupa espacio ni se encola.
    /// </summary>
    public interface ICellModifierDrop : IPlaceableDrop
    {
        // Aplica el cambio de efecto sobre la celda destino.
        void ApplyTo(Cell targetCell);
    }
}
