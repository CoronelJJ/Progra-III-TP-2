using System;
using DiceMaterials.Runtime;

namespace TurnGame.Core
{
    /// <summary>
    /// Contexto de un jugador. Guarda el estilo visual de las caras (reutiliza FaceStyleHolder)
    /// y lo expone solo a través de IFaceStyleSource (lectura) e IFaceStyleController (escritura).
    /// El broker es el del scope de este jugador, así que el cambio no afecta a otros jugadores.
    /// </summary>
    public sealed class PlayerContext : IPlayerContext, IFaceStyleSource, IFaceStyleController
    {
        private readonly FaceStyleHolder faceStyle;

        public IPlayer Player { get; }

        public PlayerContext(IPlayer player, IMessageBroker broker)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            faceStyle = new FaceStyleHolder(broker);
        }

        // IFaceStyleSource
        public FaceStyle Current => faceStyle.Current;

        // IFaceStyleController
        public void SetStyle(FaceStyle style) => faceStyle.SetStyle(style);
    }
}
