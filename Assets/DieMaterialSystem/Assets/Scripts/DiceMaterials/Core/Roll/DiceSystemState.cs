using System;

namespace DiceMaterials
{
    /// <summary>
    /// Estado guardable del sistema de tirada (v1). Campos públicos para que JsonUtility lo serialice.
    /// Los ulong se guardan como texto decimal (JsonUtility no es fiable con ulong).
    /// Con (seed + contadores) se reconstruye el estado exacto de las bolsas, sin serializarlas.
    /// </summary>
    [Serializable]
    public sealed class DiceSystemState
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string seedText;        // lo que ve el jugador
        public string seedValue;       // ulong decimal de la seed maestra
        public string fingerprint;     // ulong decimal: huella del contenido (materiales, proporciones, caras, piezas)
        public long materialsDrawn;
        public long facesDrawn;
        public long piecesDrawn;
    }
}
