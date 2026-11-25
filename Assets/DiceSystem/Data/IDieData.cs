using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Vista de solo lectura de un dado lógico. Es lo que usan puntuación, UI de resumen, etc.</summary>
    public interface IDieData : IBoardPiece
    {
        IDieMaterial Material { get; }
        IDieMaterialVisual MaterialVisual { get; }
        IFaceValue Face { get; }
        int Value { get; }

        Sprite GetFaceSprite(FaceStyle style);
    }
}
