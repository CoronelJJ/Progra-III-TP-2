using System;

namespace BoardGame.Model
{
    /// <summary>
    /// Modificador de celda concreto: asigna un nuevo efecto (o null para remover
    /// el efecto actual, quedando la celda con NullCellEffect por defecto).
    /// </summary>
    public class CellModifierDrop : ICellModifierDrop
    {
        public string InstanceId { get; }

        private readonly ICellEffect _newEffect;

        public CellModifierDrop(ICellEffect newEffect)
        {
            InstanceId = Guid.NewGuid().ToString();
            _newEffect = newEffect;
        }

        public void ApplyTo(Cell targetCell)
        {
            targetCell.SetEffect(_newEffect ?? new NullCellEffect());
        }
    }
}
