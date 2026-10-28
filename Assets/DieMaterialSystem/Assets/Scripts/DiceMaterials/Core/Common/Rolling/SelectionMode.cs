namespace DiceMaterials
{
    /// <summary>Cómo se elige un material dentro de un tier. Bag (0) es el valor por defecto.</summary>
    public enum SelectionMode : byte
    {
        /// <summary>Sin repetir hasta agotar los materiales del tier.</summary>
        Bag = 0,
        /// <summary>Azar uniforme con reposición.</summary>
        Random = 1
    }
}
