using System.Collections.Generic;

namespace DiceMaterials
{
    public interface IDieValueRoller
    {
        /// <summary>Cara máxima del dado de la partida (los valores salen en 1..MaxFace).</summary>
        int MaxFace { get; }

        int GetNextDieValue();

        /// <summary>Equivalente exacto a llamar <paramref name="quantity"/> veces a GetNextDieValue().</summary>
        IReadOnlyList<int> GetRolledDieValues(int quantity);
    }
}
