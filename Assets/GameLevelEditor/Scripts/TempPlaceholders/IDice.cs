namespace BoardGame.Model
{
    /// <summary>
    /// Dado colocable en el tablero. La cantidad de caras (D10/D12/D20) NO vive acá:
    /// se define una sola vez por partida en la configuración global del juego,
    /// ya que nunca se mezclan distintos tipos de dado base en una misma partida.
    /// </summary>
    public interface IDice : IBoardPiece
    {
        // Valor de cara actual, modificable en partida
        int FaceValue { get; set; }

        // Material del dado (leído por el evaluador para puntaje/combos), modificable en partida
        string Material { get; set; }
    }
}
