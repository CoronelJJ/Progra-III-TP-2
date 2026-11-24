using System;

namespace BoardGame.Model
{
    /// <summary>
    /// Ejemplo de efecto reactivo: destruye inmediatamente cualquier pieza
    /// que intente entrar (agujero). Permite la entrada (CanAcceptPiece = true)
    /// para poder "tragarse" la pieza recién en OnPieceEnter.
    /// </summary>
    [Serializable]
    [CellEffectInfo("Agujero", "#6A1B9A", "Acepta una pieza y la destruye al instante.")]
    public class HoleCellEffect : CellEffectBase
    {
        public override bool CanAcceptPiece(Cell cell, IBoardPiece incoming) => true;

        public override void OnPieceEnter(Cell cell, IBoardPiece piece)
        {
            // Se destruye lo que acaba de entrar, dejando la celda vacía otra vez.
            cell.DestroyPiece(piece);
        }
    }
}
