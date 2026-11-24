using VContainer;
using VContainer.Unity;

namespace TurnGame.Persistence
{
    public static class PersistenceRegistration
    {
        /// <summary>
        /// Registra el adaptador como entry point (se crea e inicializa al construir el scope y se libera con él).
        /// Requiere en el contenedor: IMessageBroker e ISaveService (builder.RegisterSaveSystem()).
        /// </summary>
        public static void RegisterSaveLoadAdapter(this IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<SaveLoadMessageAdapter>(Lifetime.Singleton);
        }
    }
}
