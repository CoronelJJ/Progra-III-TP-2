using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Base de los assets con identidad estable (materiales, piezas).
    /// El campo serializado se llama "id": mover DieMaterialSO a esta base NO invalida los assets existentes.
    /// </summary>
    public abstract class IdentifiedSO : ScriptableObject
    {
        [SerializeField, HideInInspector] private int id; // 0 = sin asignar; lo asigna la Database

        public int Id => id;

#if UNITY_EDITOR
        /// <summary>Solo Editor: lo usan las databases. No llamar desde código de juego.</summary>
        public void Editor_SetId(int newId) => id = newId;
#endif
    }
}
