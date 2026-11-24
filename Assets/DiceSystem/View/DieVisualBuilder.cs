using System;
using System.Collections.Generic;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// DieData -> Die. Es el punto de entrada para que cualquier sistema cree la vista de un dado.
    /// No posiciona ni acomoda: el layout es del sistema que muestra.
    /// </summary>
    public sealed class DieVisualBuilder
    {
        private readonly IDieFactory factory;

        public DieVisualBuilder(IDieFactory factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public Die BuildOne(DieData data) => factory.Get(data);

        /// <summary>Un Die por cada DieData, en el mismo orden.</summary>
        public List<Die> Build(IReadOnlyList<DieData> datas)
        {
            if (datas == null) throw new ArgumentNullException(nameof(datas));

            var result = new List<Die>(datas.Count);
            for (int i = 0; i < datas.Count; i++)
                result.Add(factory.Get(datas[i]));
            return result;
        }

        public void Release(Die die) => factory.Release(die);

        public void ReleaseAll(IEnumerable<Die> dice)
        {
            if (dice == null) throw new ArgumentNullException(nameof(dice));
            foreach (var die in dice) factory.Release(die);
        }
    }
}
