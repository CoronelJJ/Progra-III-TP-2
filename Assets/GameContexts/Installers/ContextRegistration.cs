using System;
using DiceMaterials.Runtime;
using VContainer;

namespace TurnGame.Core
{
    public static class ContextRegistration
    {
        /// <summary>Scope padre (general). Un solo GameContext para todos los jugadores.</summary>
        public static void RegisterGameContext(this IContainerBuilder builder, GameSettingsSO settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            builder.RegisterInstance(new GameContext(settings.DieType))
                   .As<IGameContext, IDieTypeSource>();
        }

        /// <summary>
        /// Scope hijo de UN jugador. Registra su IPlayer, su propio MessageBroker y su PlayerContext.
        /// Llamar antes de RegisterPlayerDice (DiceSystem).
        /// </summary>
        public static void RegisterPlayerContext(this IContainerBuilder builder, IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            builder.RegisterInstance(player).As<IPlayer>();
            builder.Register<MessageBroker>(Lifetime.Scoped).As<IMessageBroker>();
            builder.Register<PlayerContext>(Lifetime.Scoped)
                   .As<IPlayerContext, IFaceStyleSource, IFaceStyleController>();
        }
    }
}
