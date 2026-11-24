using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>Definición inmutable de una pieza. Solo lectura en runtime.</summary>
    [CreateAssetMenu(fileName = "NewPiece", menuName = "Dice/Piece")]
    public class PieceSO : IdentifiedSO, IPiece, IPieceVisual
    {
        [SerializeField] private string pieceName;
        [SerializeField, Min(1)] private int diceRequired = 1;
        [SerializeField] private Sprite sprite;

        // Id: heredado de IdentifiedSO
        public string Name => pieceName;
        public int DiceRequired => diceRequired;
        public Sprite Sprite => sprite;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(pieceName))
                pieceName = name;
        }
#endif
    }
}
