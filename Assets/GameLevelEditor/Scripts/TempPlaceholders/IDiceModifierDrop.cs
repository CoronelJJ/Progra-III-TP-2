using System.Collections.Generic;

namespace BoardGame.Model
{
    /// <summary>
    /// Pieza especial que, al caer sobre una celda, transforma TODOS los dados
    /// ya presentes en la cola de esa celda (no solo el de adelante).
    /// No respeta reglas de ocupación normales y nunca se encola:
    /// se aplica una vez y se descarta.
    /// </summary>
    public interface IDiceModifierDrop : IPlaceableDrop
    {
        // Aplica la transformación a cada dado presente en la cola de la celda destino.
        void ApplyTo(IEnumerable<IDice> diceInCell);
    }
}
