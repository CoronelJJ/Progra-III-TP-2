using System.Collections.Generic;

namespace DiceMaterials
{
    public interface IDieMaterialRoller
    {
        IDieMaterial GetNextDieMaterial();

        /// <summary>Equivalente exacto a llamar <paramref name="quantity"/> veces a GetNextDieMaterial().</summary>
        IReadOnlyList<IDieMaterial> GetRolledDieMaterials(int quantity);
    }
}
