using System;

namespace BoardGame.Model
{
    /// <summary>
    /// "Roca": la celda es jugable (forma parte del nivel) pero está ocupada
    /// y no puede recibir piezas nuevas. Es distinto de una celda inactiva,
    /// que directamente está fuera de la forma del nivel.
    /// </summary>
    [Serializable]
    [CellEffectInfo("Bloqueada (roca)", "#8D6E63", "Celda jugable que rechaza cualquier pieza nueva.")]
    public class BlockedCellEffect : CellEffectBase
    {
        public override bool CanAcceptPiece(Cell cell, IBoardPiece incoming) => false;
    }
}
