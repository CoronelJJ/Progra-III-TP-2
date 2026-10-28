namespace DiceMaterials
{
    /// <summary>
    /// Slot fijo de cada roller dentro de la seed maestra.
    /// REGLA: solo se agregan slots NUEVOS AL FINAL. Nunca reordenar ni renumerar,
    /// o las seeds guardadas cambian de significado.
    /// </summary>
    public enum RollerSlot
    {
        Material = 0,
        DieValue = 1,
        Piece = 2
    }
}
