using System;
using System.Collections.Generic;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Punto único para recrear dados desde un guardado: save -> DieData -> Die.
    /// Si ya tienes DieData, usa DieVisualBuilder directamente.
    /// </summary>
    public sealed class DieRecreator
    {
        private readonly DieSaveMapper mapper;
        private readonly DieVisualBuilder visualBuilder;

        public DieRecreator(DieSaveMapper mapper, DieVisualBuilder visualBuilder)
        {
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.visualBuilder = visualBuilder ?? throw new ArgumentNullException(nameof(visualBuilder));
        }

        /// <summary>Solo la parte lógica (sin GameObjects).</summary>
        public List<DieData> RestoreData(DieSaveBundle bundle) => mapper.FromSave(bundle);

        /// <summary>Lógica + vista. data[i] corresponde a dice[i].</summary>
        public List<Die> RestoreWithViews(DieSaveBundle bundle, out List<DieData> data)
        {
            data = mapper.FromSave(bundle);
            return visualBuilder.Build(data);
        }

        /// <summary>Recrea la vista de un DieData existente (p. ej. al reubicar dados).</summary>
        public Die RecreateView(DieData data) => visualBuilder.BuildOne(data);
    }
}
