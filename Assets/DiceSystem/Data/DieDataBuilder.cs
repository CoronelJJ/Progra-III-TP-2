using System;
using System.Collections.Generic;

namespace DiceMaterials.Runtime
{
    /// <summary>Convierte (materiales, ints) del sorteador en DieData. Listas emparejadas por posición.</summary>
    public sealed class DieDataBuilder
    {
        private readonly IFaceValueProvider faceProvider;
        private readonly IDieTypeSource dieType;

        public DieDataBuilder(IFaceValueProvider faceProvider, IDieTypeSource dieType)
        {
            this.faceProvider = faceProvider ?? throw new ArgumentNullException(nameof(faceProvider));
            this.dieType = dieType ?? throw new ArgumentNullException(nameof(dieType));
        }

        public List<DieData> Build(IReadOnlyList<IDieMaterial> materials, IReadOnlyList<int> values)
        {
            if (materials == null) throw new ArgumentNullException(nameof(materials));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (materials.Count != values.Count)
                throw new ArgumentException(
                    $"Largos distintos: {materials.Count} materiales y {values.Count} valores.");

            int maxValue = dieType.FaceCount;
            var result = new List<DieData>(materials.Count);

            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i] == null)
                    throw new ArgumentException($"El material {i} es null.", nameof(materials));

                if (!(materials[i] is DieMaterialSO materialSO))
                    throw new ArgumentException(
                        $"El material {i} ('{materials[i].Name}') es {materials[i].GetType().Name}, se esperaba DieMaterialSO.",
                        nameof(materials));

                int value = values[i];
                if (value < 1 || value > maxValue)
                    throw new ArgumentOutOfRangeException(nameof(values),
                        $"El valor {value} (posición {i}) está fuera del rango 1..{maxValue} del dado.");

                if (!faceProvider.TryGet(value, out var face))
                    throw new ArgumentException($"No existe FaceValueSO para el valor {value} (posición {i}).", nameof(values));

                result.Add(new DieData(materialSO, face));
            }

            return result;
        }
    }
}
