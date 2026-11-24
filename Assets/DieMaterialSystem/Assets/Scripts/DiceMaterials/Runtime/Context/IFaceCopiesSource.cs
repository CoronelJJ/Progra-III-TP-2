namespace DiceMaterials.Runtime
{
    /// <summary>Copias de cada cara en la bolsa de valores (>= 1).</summary>
    public interface IFaceCopiesSource
    {
        int CopiesPerFace { get; }
    }
}
