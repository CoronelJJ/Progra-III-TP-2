using System.Collections.Generic;

namespace DiceMaterials
{
    public interface IPieceRoller
    {
        /// <summary>Cantidad de piezas entregadas desde la construcción (sirve para guardar/restaurar).</summary>
        long DrawnCount { get; }

        IPiece GetNextPiece();

        /// <summary>Equivalente exacto a llamar <paramref name="quantity"/> veces a GetNextPiece().</summary>
        IReadOnlyList<IPiece> GetRolledPieces(int quantity);
    }
}
