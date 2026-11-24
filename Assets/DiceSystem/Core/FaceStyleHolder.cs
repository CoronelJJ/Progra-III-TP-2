using System;
using TurnGame.Core;

namespace DiceMaterials.Runtime
{
    /// <summary>Lectura del estilo vigente. Lo consulta un dado al nacer o al salir del pool.</summary>
    public interface IFaceStyleSource
    {
        FaceStyle Current { get; }
    }

    /// <summary>Escritura del estilo. Solo la usa quien decide el cambio (efectos, etc.).</summary>
    public interface IFaceStyleController
    {
        void SetStyle(FaceStyle style);
    }

    /// <summary>
    /// Guarda el estilo actual y publica el cambio en el broker del Player.
    /// Un broker por scope de Player, así que el mensaje no lleva id de jugador.
    /// Constructor sin parámetros opcionales para que VContainer pueda resolverlo;
    /// el estilo inicial es Arabic (cámbialo con SetStyle si hace falta).
    /// </summary>
    public sealed class FaceStyleHolder : IFaceStyleSource, IFaceStyleController
    {
        private readonly IMessageBroker broker;

        public FaceStyle Current { get; private set; } = FaceStyle.Arabic;

        public FaceStyleHolder(IMessageBroker broker)
        {
            this.broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        public void SetStyle(FaceStyle style)
        {
            if (style == Current) return;
            Current = style;
            broker.Publish(new FaceStyleChanged(style));
        }
    }
}
