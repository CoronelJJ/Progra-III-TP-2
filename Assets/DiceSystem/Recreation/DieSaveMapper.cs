using System;
using System.Collections.Generic;

namespace DiceMaterials.Runtime
{
    /// <summary>DieData <-> DieSaveData. Reutiliza DieDataBuilder, así que valida igual que el sorteo.</summary>
    public sealed class DieSaveMapper
    {
        private readonly IDieMaterialProvider materials;
        private readonly DieDataBuilder dataBuilder;

        public DieSaveMapper(IDieMaterialProvider materials, DieDataBuilder dataBuilder)
        {
            this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
            this.dataBuilder = dataBuilder ?? throw new ArgumentNullException(nameof(dataBuilder));
        }

        public DieSaveData ToSave(IDieData die)
        {
            if (die == null) throw new ArgumentNullException(nameof(die));
            return new DieSaveData(die.Material.Id, die.Value);
        }

        public DieSaveBundle ToSave(IReadOnlyList<IDieData> dice)
        {
            if (dice == null) throw new ArgumentNullException(nameof(dice));
            var bundle = new DieSaveBundle { dice = new DieSaveData[dice.Count] };
            for (int i = 0; i < dice.Count; i++) bundle.dice[i] = ToSave(dice[i]);
            return bundle;
        }

        public DieData FromSave(DieSaveData save) => FromSave(new[] { save })[0];

        public List<DieData> FromSave(IReadOnlyList<DieSaveData> saves)
        {
            if (saves == null) throw new ArgumentNullException(nameof(saves));

            var mats = new List<IDieMaterial>(saves.Count);
            var values = new List<int>(saves.Count);

            for (int i = 0; i < saves.Count; i++)
            {
                if (!materials.TryGet(saves[i].materialId, out var material))
                    throw new ArgumentException(
                        $"Guardado inválido: no existe material con Id {saves[i].materialId} (posición {i}).");
                mats.Add(material);
                values.Add(saves[i].faceValue);
            }

            return dataBuilder.Build(mats, values);
        }

        public List<DieData> FromSave(DieSaveBundle bundle)
        {
            if (bundle == null) throw new ArgumentNullException(nameof(bundle));
            return FromSave(bundle.dice);
        }
    }
}
