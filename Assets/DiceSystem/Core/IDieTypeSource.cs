namespace DiceMaterials.Runtime
{
    /// <summary>Tipo de dado de la partida (6 o 12 caras). Config del GameContext general, fija y de solo lectura.</summary>
    public interface IDieTypeSource
    {
        int FaceCount { get; }
    }
}
