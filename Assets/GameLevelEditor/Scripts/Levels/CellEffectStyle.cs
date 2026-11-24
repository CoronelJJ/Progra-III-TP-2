using System;
using System.Collections.Generic;
using BoardGame.Model;
using UnityEngine;

namespace BoardGame.Levels
{
    /// <summary>
    /// Nombre, color y descripción de cada tipo de efecto, leídos del atributo
    /// [CellEffectInfo] (o generados automáticamente si no lo tiene).
    /// Lo usan tanto el editor de niveles como la vista de runtime, para que los colores coincidan.
    /// </summary>
    public static class CellEffectStyle
    {
        public static readonly Color UnplayableColor = new Color(0.13f, 0.13f, 0.16f, 1f);
        public static readonly Color NoEffectColor = new Color(0.82f, 0.84f, 0.88f, 1f);

        private class Entry
        {
            public string Name;
            public string Description;
            public Color Color;
        }

        private static readonly Dictionary<Type, Entry> Cache = new Dictionary<Type, Entry>();

        public static string GetDisplayName(Type type) => Get(type).Name;

        public static string GetDescription(Type type) => Get(type).Description;

        public static Color GetColor(Type type) => Get(type).Color;

        /// <summary>Color para una celda según su efecto (null o NullCellEffect = sin efecto).</summary>
        public static Color GetColor(ICellEffect effect)
        {
            if (effect == null || effect is NullCellEffect) return NoEffectColor;
            return GetColor(effect.GetType());
        }

        private static Entry Get(Type type)
        {
            if (type == null) return new Entry { Name = "(sin tipo)", Description = "", Color = NoEffectColor };

            Entry entry;
            if (Cache.TryGetValue(type, out entry)) return entry;

            var info = GetInfo(type);
            entry = new Entry
            {
                Name = (info != null && !string.IsNullOrEmpty(info.DisplayName)) ? info.DisplayName : PrettyName(type),
                Description = (info != null && info.Description != null) ? info.Description : "",
                Color = ResolveColor(type, info)
            };
            Cache[type] = entry;
            return entry;
        }

        private static CellEffectInfoAttribute GetInfo(Type type)
        {
            object[] attrs = type.GetCustomAttributes(typeof(CellEffectInfoAttribute), false);
            return attrs.Length > 0 ? (CellEffectInfoAttribute)attrs[0] : null;
        }

        private static string PrettyName(Type type)
        {
            const string suffix = "CellEffect";
            string name = type.Name;
            if (name.EndsWith(suffix) && name.Length > suffix.Length)
                name = name.Substring(0, name.Length - suffix.Length);
            return name;
        }

        private static Color ResolveColor(Type type, CellEffectInfoAttribute info)
        {
            if (type == typeof(NullCellEffect)) return NoEffectColor;

            Color parsed;
            if (info != null && !string.IsNullOrEmpty(info.ColorHex) &&
                ColorUtility.TryParseHtmlString(info.ColorHex, out parsed))
                return parsed;

            // Color automático y estable derivado del nombre del tipo.
            int hash = 17;
            string full = type.FullName ?? type.Name;
            for (int i = 0; i < full.Length; i++) hash = hash * 31 + full[i];
            float hue = (Mathf.Abs(hash) % 360) / 360f;
            return Color.HSVToRGB(hue, 0.55f, 0.9f);
        }
    }
}
