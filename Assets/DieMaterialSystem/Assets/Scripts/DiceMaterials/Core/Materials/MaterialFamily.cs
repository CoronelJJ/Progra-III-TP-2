using System;

namespace DiceMaterials
{
    /// <summary>
    /// Familia del material (Flags: un material puede pertenecer a varias).
    /// Los valores ya asignados NO deben renumerarse: se serializan en los assets.
    /// Agregar nuevas familias siempre con la siguiente potencia de 2.
    /// </summary>
    [Flags]
    public enum MaterialFamily
    {
        None  = 0,
        Color = 1 << 0,
        Metal = 1 << 1,
        Gem   = 1 << 2
    }
}
