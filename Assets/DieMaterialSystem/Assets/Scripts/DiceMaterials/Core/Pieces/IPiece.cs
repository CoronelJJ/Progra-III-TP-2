namespace DiceMaterials
{
    /// <summary>Pieza tipo Tetris. Solo lo que necesita la bolsa; la forma/celdas las define quien la implemente.</summary>
    public interface IPiece
    {
        /// <summary>Identidad estable y única dentro del conjunto de piezas. Se usa para ordenar antes de mezclar.</summary>
        int Id { get; }
        string Name { get; }
    }
}
