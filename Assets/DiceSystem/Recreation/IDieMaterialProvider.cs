namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Material por Id estable. Impleméntalo con tu DieMaterialDatabase (o equivalente).
    /// </summary>
    public interface IDieMaterialProvider
    {
        bool TryGet(int id, out DieMaterialSO material);
    }
}
