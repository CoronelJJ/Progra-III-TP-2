using System.Collections.Generic;

namespace DiceMaterials
{
    public interface IDieMaterialRoller
    {
        /// <summary>Cantidad de materiales entregados desde la construcción (sirve para guardar/restaurar).</summary>
        long DrawnCount { get; }

        IDieMaterial GetNextDieMaterial();

        /// <summary>Equivalente exacto a llamar <paramref name="quantity"/> veces a GetNextDieMaterial().</summary>
        IReadOnlyList<IDieMaterial> GetRolledDieMaterials(int quantity);
    }
}
