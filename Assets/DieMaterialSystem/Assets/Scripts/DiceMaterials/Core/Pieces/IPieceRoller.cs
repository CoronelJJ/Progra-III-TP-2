using System.Collections.Generic;

namespace DiceMaterials
{
    public interface IPieceRoller
    {
        IPiece GetNextPiece();

        /// <summary>Equivalente exacto a llamar <paramref name="quantity"/> veces a GetNextPiece().</summary>
        IReadOnlyList<IPiece> GetRolledPieces(int quantity);
    }
}
