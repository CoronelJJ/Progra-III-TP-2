using System;

namespace DiceMaterials
{
    /// <summary>
    /// Raíz del guardado. Cada sistema nuevo agrega acá su campo [Serializable] y un ISaveContributor.
    /// Agregar campos es compatible hacia atrás; cambiar/quitar/renombrar exige subir CurrentVersion.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public DiceSystemState dice;
    }
}
