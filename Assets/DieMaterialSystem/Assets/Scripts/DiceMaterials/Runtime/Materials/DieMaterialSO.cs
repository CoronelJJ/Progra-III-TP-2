using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Definición inmutable de un material. Solo lectura en runtime:
    /// lo que "cambia en juego" es la referencia que tiene el dado, nunca este asset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDieMaterial", menuName = "Dice/Die Material")]
    public class DieMaterialSO : ScriptableObject, IDieMaterial, IDieMaterialVisual
    {
        [SerializeField, HideInInspector] private int id; // lo asigna DieMaterialDatabase (0 = sin asignar)
        [SerializeField] private string materialName;
        [SerializeField] private MaterialTier tier = MaterialTier.C;
        [SerializeField] private MaterialFamily family = MaterialFamily.None;
        [SerializeField] private int score = 1;
        [SerializeField, Min(0f)] private float multiplier = 1f;
        [SerializeField] private Material unityMaterial;

        public int Id => id;
        public string Name => materialName;
        public MaterialTier Tier => tier;
        public MaterialFamily Family => family;
        public int Score => score;
        public float Multiplier => multiplier;
        public int FinalScore => DieMaterialMath.ComputeFinalScore(score, multiplier);
        public Material UnityMaterial => unityMaterial;

#if UNITY_EDITOR
        /// <summary>Solo Editor: lo usa DieMaterialDatabase. No llamar desde código de juego.</summary>
        public void Editor_SetId(int newId) => id = newId;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(materialName))
                materialName = name;
        }
#endif
    }
}
