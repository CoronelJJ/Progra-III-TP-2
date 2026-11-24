using DiceMaterials;

namespace DiceMaterials.Runtime
{
    /// <summary>Seed maestra de la partida, ya resuelta (la define la escena de selección y no cambia mientras dura).</summary>
    public interface ISeedSource
    {
        GameSeed Seed { get; }
    }
}
