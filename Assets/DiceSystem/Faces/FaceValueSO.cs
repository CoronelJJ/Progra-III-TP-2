using System;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    [Serializable]
    public struct FaceVariant
    {
        public FaceStyle style;
        public Sprite sprite;
    }

    /// <summary>
    /// Un valor lógico (ej. "3") con todas sus representaciones visuales.
    /// Solo lectura en runtime: el estilo activo vive en el FaceStyleHolder, nunca aquí.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFaceValue", menuName = "Dice/Face Value")]
    public class FaceValueSO : ScriptableObject, IFaceValue
    {
        [SerializeField] private int value = 1;
        [SerializeField] private FaceStyle defaultStyle = FaceStyle.Arabic;
        [SerializeField] private FaceVariant[] variants = Array.Empty<FaceVariant>();

        public int Value => value;
        public FaceStyle DefaultStyle => defaultStyle;

        public bool TryGetSprite(FaceStyle style, out Sprite sprite)
        {
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i].style == style && variants[i].sprite != null)
                {
                    sprite = variants[i].sprite;
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        public Sprite GetSprite(FaceStyle style)
        {
            if (TryGetSprite(style, out var sprite)) return sprite;
            if (style != defaultStyle && TryGetSprite(defaultStyle, out sprite)) return sprite;
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (variants == null) return;

            bool hasDefault = false;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i].sprite == null)
                    Debug.LogWarning($"[{name}] Variante {i} ({variants[i].style}) sin sprite.", this);

                if (variants[i].style == defaultStyle && variants[i].sprite != null)
                    hasDefault = true;

                for (int j = i + 1; j < variants.Length; j++)
                {
                    if (variants[i].style == variants[j].style)
                        Debug.LogWarning($"[{name}] Estilo duplicado: {variants[i].style}.", this);
                }
            }

            if (!hasDefault)
                Debug.LogWarning($"[{name}] No hay sprite para el estilo por defecto ({defaultStyle}); el fallback no funcionará.", this);
        }
#endif
    }
}
