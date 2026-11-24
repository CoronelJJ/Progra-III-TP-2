namespace BoardGame.Model
{
    /// <summary>
    /// Contrato de un efecto de celda, basado en "hooks" que reaccionan a momentos
    /// del flujo de juego. Cada efecto concreto sobreescribe solo lo que le importa.
    /// </summary>
    public interface ICellEffect
    {
        // Se llama cuando una pieza se encola en la celda (nueva o stack).
        void OnPieceEnter(Cell cell, IBoardPiece piece);

        // Se llama cuando una pieza sale de la cola (por jugada resuelta o borrado).
        void OnPieceExit(Cell cell, IBoardPiece piece);

        // Se llama durante la evaluación de jugada, para transformar el valor leído.
        int ModifyValue(int baseValue);

        // Determina si la celda puede aceptar una pieza nueva en este momento.
        bool CanAcceptPiece(Cell cell, IBoardPiece incoming);

        // Determina si la celda puede aceptar un modificador (punto de extensión futuro,
        // ej: un efecto tipo "escudo" que bloquee modificadores).
        bool CanAcceptModifier(Cell cell);

        // Se llama cuando un efecto de área (bomba) impacta la celda.
        void OnBombHit(Cell cell, object bombInfo);

        // Permite modificar el límite base de stacking para esta celda puntual.
        int ModifyStackLimit(int baseLimit);
    }
}
