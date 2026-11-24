namespace BoardGame.Model
{
    /// <summary>
    /// Contrato mínimo de cualquier cosa que se pueda "soltar" sobre una celda:
    /// un dado, un modificador de dado o un modificador de celda.
    /// No implica que ocupe espacio en el tablero (eso lo agrega IBoardPiece).
    /// </summary>
    public interface IPlaceableDrop
    {
        // Identificador único de esta instancia (útil para trackeo, debug, guardado)
        string InstanceId { get; }
    }
}
