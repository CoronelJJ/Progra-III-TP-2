using UnityEngine;

namespace DiceMaterials.Runtime
{
    public interface IFaceValue
    {
        /// <summary>Valor lógico de la cara (el que cuenta para el juego).</summary>
        int Value { get; }

        /// <summary>Estilo usado como fallback si falta la variante pedida.</summary>
        FaceStyle DefaultStyle { get; }

        /// <summary>Sprite del estilo pedido, o del estilo por defecto si no existe. Null si ninguno está definido.</summary>
        Sprite GetSprite(FaceStyle style);

        bool TryGetSprite(FaceStyle style, out Sprite sprite);
    }
}
