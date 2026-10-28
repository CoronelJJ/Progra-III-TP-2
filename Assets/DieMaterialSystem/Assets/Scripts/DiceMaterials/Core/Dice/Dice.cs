using System;
using DiceMaterials;

namespace BoardGame.Model
{
    /// <summary>
    /// Implementación concreta y básica de un dado.
    /// </summary>
    public class Dice : IDice
    {
       
        public int FaceValue { get; set; }
        public IDieMaterial CurrentMaterial { get; private set; }

        public Dice(int faceValue, IDieMaterial materialData)
        {
            
            FaceValue = faceValue;
            CurrentMaterial = materialData;
        }

    }
}
