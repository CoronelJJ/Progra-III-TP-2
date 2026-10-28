using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Parte visual del material. Solo la usa quien renderiza (Die), nunca la lógica.</summary>
    public interface IDieMaterialVisual
    {
        Material UnityMaterial { get; }
    }
}
