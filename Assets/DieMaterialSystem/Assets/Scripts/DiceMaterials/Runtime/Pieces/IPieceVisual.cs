using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Parte visual de la pieza. Solo la usa quien la dibuja, nunca la lógica.</summary>
    public interface IPieceVisual
    {
        Sprite Sprite { get; }
    }
}
