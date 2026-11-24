using System;
using UnityEngine;

namespace BoardGame.Model
{
    /// <summary>
    /// Ejemplo de efecto simple: multiplica (positivo o negativo) el valor leído
    /// por el evaluador. No reacciona a ningún otro hook.
    ///
    /// Cambios para el editor: el campo ya no es readonly sino un [SerializeField] privado
    /// (Unity no serializa campos readonly) y hay un constructor sin parámetros,
    /// requerido para crear el efecto desde el editor.
    /// </summary>
    [Serializable]
    [CellEffectInfo("Multiplicador", "#F9A825", "Multiplica el valor del dado leído por el evaluador.")]
    public class MultiplierCellEffect : CellEffectBase
    {
        [SerializeField] private int _factor = 2;

        public MultiplierCellEffect() { }

        public MultiplierCellEffect(int factor)
        {
            _factor = factor;
        }

        public override int ModifyValue(int baseValue) => baseValue * _factor;
    }
}
