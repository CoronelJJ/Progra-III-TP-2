using System;
using DiceMaterials;
using DiceMaterials.Runtime;

namespace TurnGame.Core
{
    /// <summary>
    /// Contexto general: vive en el scope padre (el de la máquina de estados / partida).
    /// Lo construye la escena de selección. Implementa interfaces estrechas para que cada sistema pida solo lo que necesita.
    /// </summary>
    public sealed class GameContext : IGameContext, IDieTypeSource, IFaceCopiesSource, ISeedSource
    {
        private DieType dieType;


        public DieType DieType { get; }
        public int CopiesPerFace { get; }
        public GameSeed Seed { get; }
        public string LoadSlot { get; }

        // IDieTypeSource (DiceSystem)
        public int FaceCount => (int)DieType;

        /// <param name="seed">Seed ya resuelta (GameSeed.FromInput en partida nueva, o la leída del guardado al continuar).</param>
        /// <param name="loadSlot">Slot a cargar al arrancar; null o vacío = partida nueva.</param>
        public GameContext(DieType dieType, int copiesPerFace, GameSeed seed, string loadSlot = null)
        {
            if (!Enum.IsDefined(typeof(DieType), dieType))
                throw new ArgumentOutOfRangeException(nameof(dieType), dieType, "Tipo de dado no válido.");
            if (copiesPerFace < 1)
                throw new ArgumentOutOfRangeException(nameof(copiesPerFace), copiesPerFace, "Debe haber al menos 1 copia por cara.");
            if (seed.Text == null)
                throw new ArgumentException("La seed no está inicializada (usar GameSeed.FromInput / FromText / Generate).", nameof(seed));

            DieType = dieType;
            CopiesPerFace = copiesPerFace;
            Seed = seed;
            LoadSlot = string.IsNullOrWhiteSpace(loadSlot) ? null : loadSlot;
        }

        public GameContext(DieType dieType)
        {
            this.dieType = dieType;
        }

    }
}
