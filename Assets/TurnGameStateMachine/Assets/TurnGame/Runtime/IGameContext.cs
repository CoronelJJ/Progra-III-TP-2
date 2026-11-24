using DiceMaterials;

namespace TurnGame.Core
{
    /// <summary>
    /// Estado y config GLOBAL de la partida (igual para todos los jugadores).
    /// Los sistemas no deberían depender de esta interfaz completa si les alcanza una más estrecha
    /// (p. ej. IDieTypeSource). Se amplía agregando miembros aquí y la interfaz estrecha en la clase.
    /// Requiere que el asmdef TurnGame.Core referencie "DiceMaterials.Core" (por GameSeed).
    /// </summary>
    public interface IGameContext
    {
        /// <summary>Tipo de dado de la partida. No cambia mientras dura.</summary>
        DieType DieType { get; }

        /// <summary>Copias de cada cara en la bolsa de valores del dado (&gt;= 1). No cambia mientras dura la partida.</summary>
        int CopiesPerFace { get; }

        /// <summary>Seed maestra de la partida, resuelta en la escena de selección. No cambia mientras dura.</summary>
        GameSeed Seed { get; }

        /// <summary>Slot del guardado a cargar al arrancar. Null o vacío = partida nueva.</summary>
        string LoadSlot { get; }
    }
}
