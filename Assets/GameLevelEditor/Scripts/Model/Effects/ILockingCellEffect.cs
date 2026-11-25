/// <summary>
/// Marca a un efecto de celda como "bloqueante": mientras IsLocked sea true,
/// Cell.SetEffect no lo reemplaza. Lo usa ObstacleLockCellEffect.
/// </summary>
public interface ILockingCellEffect
{
    bool IsLocked { get; }
}
