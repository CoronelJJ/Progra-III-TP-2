using System;

namespace BoardGame.Model
{
    /// <summary>
    /// Efecto "vacío": el que tiene por defecto cualquier celda sin modificar.
    /// Evita chequeos de null repartidos por todo el código (patrón Null Object).
    /// El editor de niveles lo oculta de la paleta: "sin efecto" = null en el blueprint.
    /// </summary>
    [Serializable]
    public class NullCellEffect : CellEffectBase
    {
        // No sobreescribe nada: todos los hooks quedan con el comportamiento neutro.
    }
}
