namespace DiceMaterials
{
    /// <summary>Un sistema que aporta (Capture) y recupera (Restore) su parte del guardado.</summary>
    public interface ISaveContributor
    {
        void Capture(GameSaveData data);
        void Restore(GameSaveData data);
    }
}
