namespace DiceMaterials
{
    /// <summary>Pieza tipo Tetris. La forma/celdas las interpreta otro sistema; acá solo lo que necesita la bolsa y el resultado.</summary>
    public interface IPiece
    {
        /// <summary>Identidad estable y única dentro del conjunto de piezas. Se usa para ordenar antes de mezclar.</summary>
        int Id { get; }
        string Name { get; }
        /// <summary>Cantidad de dados que consume la pieza.</summary>
        int DiceRequired { get; }
    }
}
