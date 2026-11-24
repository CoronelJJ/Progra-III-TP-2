using UnityEngine;

namespace TurnGame.Core
{
    /// <summary>Configuración fija de la partida, editable en el inspector. Solo lectura en runtime.</summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [SerializeField] private DieType dieType = DieType.D6;
        [Tooltip("Copias de cada cara en la bolsa de valores. Con 1, cada mano de N=caras dados sería una permutación (sin pares).")]
        [SerializeField, Min(1)] private int copiesPerFace = 4;

        public DieType DieType => dieType;
        public int CopiesPerFace => copiesPerFace;
    }
}
