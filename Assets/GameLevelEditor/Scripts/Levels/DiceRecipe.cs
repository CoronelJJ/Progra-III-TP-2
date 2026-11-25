using System;
using DiceMaterials.Runtime;

/// <summary>
/// "Receta" de un dado precolocado: material + cara exacta.
/// Es lo que guarda el blueprint; el LevelBuilder la convierte en un DieData real.
/// </summary>
[Serializable]
public class DiceRecipe
{
    public DieMaterialSO material;
    public FaceValueSO face;
}
