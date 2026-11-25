//namespace BoardGame.Model
//{
    /// <summary>
    /// Algo que efectivamente ocupa un lugar en la cola de una celda
    /// (a diferencia de los modificadores, que se consumen sin ocupar espacio).
    /// </summary>
    public interface IBoardPiece : IPlaceableDrop
    {
        // Determina si esta pieza puede apilarse con otra ya presente en la celda.
        // Para dados: solo importa que compartan el mismo valor de cara.
        bool CanStackWith(IBoardPiece other);
    }
//}
