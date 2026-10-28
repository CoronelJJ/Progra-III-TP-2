using DiceMaterials;

namespace BoardGame.Model
{
    /// <summary>Parte lógica de un dado: su valor de cara y el material con el que salió.</summary>
    public interface IDice
    {
        int FaceValue { get; }
        IDieMaterial CurrentMaterial { get; }
    }
}
