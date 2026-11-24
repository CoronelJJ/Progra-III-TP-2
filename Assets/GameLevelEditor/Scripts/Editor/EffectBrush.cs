using BoardGame.Model;
using UnityEngine;

namespace BoardGame.Levels.EditorTools
{
    /// <summary>
    /// Contenedor temporal (solo editor) del efecto que se "pinta" con el pincel.
    /// Existe para poder mostrar y editar los parámetros del efecto con SerializedObject.
    /// </summary>
    public class EffectBrush : ScriptableObject
    {
        [SerializeReference] public ICellEffect effect;
    }
}
