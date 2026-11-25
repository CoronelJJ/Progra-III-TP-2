using System;
using System.Collections.Generic;
using BoardGame.Model;

/// <summary>
/// Efecto que crea el LevelBuilder en celdas con dados precolocados (obstáculos).
/// Mientras quede algún obstáculo en la celda:
///  - no acepta dados nuevos,
///  - no acepta modificadores (los obstáculos son inmutables por ahora),
///  - Cell.SetEffect no puede reemplazarlo (ILockingCellEffect).
/// Cuando sale el ÚLTIMO obstáculo (jugada, bomba, destrucción...) la celda vuelve a NullCellEffect.
///
/// No aparece en la paleta del editor: no tiene constructor sin parámetros y se crea solo desde código.
/// Solo vigila los dados que se le registran, así que no altera las reglas normales de apilado.
/// </summary>
[CellEffectInfo("Obstáculo", "#546E7A", "Celda con dados precolocados: bloqueada hasta quedar vacía.")]
public class ObstacleLockCellEffect : CellEffectBase, ILockingCellEffect
{
    private readonly HashSet<IBoardPiece> _remaining = new HashSet<IBoardPiece>();

    /// <summary>Se dispara cuando sale un obstáculo (ya con la celda liberada si era el último).</summary>
    public event Action<IBoardPiece> ObstacleRemoved;

    public ObstacleLockCellEffect(IEnumerable<IBoardPiece> obstacles)
    {
        if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
        foreach (IBoardPiece piece in obstacles)
            if (piece != null) _remaining.Add(piece);
    }

    public int Remaining => _remaining.Count;

    public bool IsLocked => _remaining.Count > 0;

    public  bool CanAcceptPiece(Cell cell, IBoardPiece incoming) => !IsLocked;

    // Hoy los obstáculos son inmutables: ningún modificador entra mientras estén.
    public override bool CanAcceptModifier(Cell cell) => !IsLocked;

    public  void OnPieceExit(Cell cell, IBoardPiece piece)
    {
        if (!_remaining.Remove(piece)) return;

        // Primero se libera la celda y después se avisa, para que quien escuche vea el estado final.
        if (!IsLocked) cell.SetEffect(new NullCellEffect());
        ObstacleRemoved?.Invoke(piece);
    }
}
