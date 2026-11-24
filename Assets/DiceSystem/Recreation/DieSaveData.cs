using System;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Lo mínimo para recrear un dado: ids estables, sin referencias a assets.
    /// Serializable con JsonUtility. El estilo de cara NO se guarda aquí: es estado del Player.
    /// </summary>
    [Serializable]
    public struct DieSaveData
    {
        public int materialId;
        public int faceValue;

        public DieSaveData(int materialId, int faceValue)
        {
            this.materialId = materialId;
            this.faceValue = faceValue;
        }
    }

    /// <summary>Envoltorio para guardar una lista (JsonUtility no serializa listas en la raíz).</summary>
    [Serializable]
    public class DieSaveBundle
    {
        public DieSaveData[] dice = Array.Empty<DieSaveData>();
    }
}
