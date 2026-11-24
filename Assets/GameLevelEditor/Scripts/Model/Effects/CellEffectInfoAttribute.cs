using System;

namespace BoardGame.Model
{
    /// <summary>
    /// Atributo OPCIONAL para dar nombre, color y descripción a un efecto en el editor de niveles.
    /// Si un efecto no lo tiene, el editor usa el nombre de la clase y un color automático.
    /// El color se escribe en hexadecimal (ej: "#FF8800") porque los atributos no admiten Color.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CellEffectInfoAttribute : Attribute
    {
        public string DisplayName { get; }
        public string ColorHex { get; }
        public string Description { get; }

        public CellEffectInfoAttribute(string displayName, string colorHex = null, string description = null)
        {
            DisplayName = displayName;
            ColorHex = colorHex;
            Description = description;
        }
    }
}
