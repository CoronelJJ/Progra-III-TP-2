using BoardGame.Model;

/// <summary>Consultas sobre los obstáculos precolocados de un Board (para la condición de victoria).</summary>
public static class ObstacleUtility
{
    /// <summary>Cantidad de dados obstáculo que quedan en todo el tablero.</summary>
    public static int CountRemaining(Board board)
    {
        int total = 0;
        foreach (Cell cell in board.AllActiveCells())
        {
            var obstacle = cell.Effect as ObstacleLockCellEffect;
            if (obstacle != null) total += obstacle.Remaining;
        }
        return total;
    }

    /// <summary>true si no queda ningún obstáculo (también si el nivel nunca tuvo).</summary>
    public static bool AllCleared(Board board) => CountRemaining(board) == 0;
}
