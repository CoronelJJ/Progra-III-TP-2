using System;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Modelo lógico de un dado: material + cara actual. Fuente de verdad para el guardado.
    /// Sin eventos: los cambios individuales pasan siempre por Die.SetFace / Die.SetMaterial,
    /// que actualizan este dato y luego la vista.
    /// </summary>
    public sealed class DieData : IDieData
    {
        private DieMaterialSO material;
        private FaceValueSO face;

        public DieData(DieMaterialSO material, FaceValueSO face)
        {
            this.material = material ? material : throw new ArgumentNullException(nameof(material));
            this.face = face ? face : throw new ArgumentNullException(nameof(face));
        }

        public IDieMaterial Material => material;
        public IDieMaterialVisual MaterialVisual => material;
        public IFaceValue Face => face;
        public int Value => face.Value;

        public string InstanceId => throw new NotImplementedException();

        public bool CanStackWith(IBoardPiece other)
        {
            throw new NotImplementedException();
        }


        public Sprite GetFaceSprite(FaceStyle style) => face.GetSprite(style);

        /// <summary>Devuelve true si hubo cambio (idempotente: el mismo SO no hace nada).</summary>
        public bool SetFace(FaceValueSO newFace)
        {
            if (newFace == null) throw new ArgumentNullException(nameof(newFace));
            if (newFace == face) return false;
            face = newFace;
            return true;
        }

        /// <summary>Devuelve true si hubo cambio (idempotente: el mismo SO no hace nada).</summary>
        public bool SetMaterial(DieMaterialSO newMaterial)
        {
            if (newMaterial == null) throw new ArgumentNullException(nameof(newMaterial));
            if (newMaterial == material) return false;
            material = newMaterial;
            return true;
        }
    }
}
