using System;
using DiceMaterials;
using TurnGame.Core;
using VContainer.Unity;

namespace TurnGame.Persistence
{
    /// <summary>
    /// Puente entre el broker y el guardado: escucha SaveRequested / LoadRequested y responde con
    /// GameSaved / SaveFailed / GameLoaded / LoadFailed. Nunca deja escapar una excepción del guardado:
    /// la convierte en el mensaje de fallo.
    ///
    /// Los mensajes de resultado se publican FUERA del try: si un suscriptor de GameSaved lanza,
    /// no se reporta como si el guardado hubiera fallado.
    /// Debe vivir en el mismo scope que el SaveService.
    /// </summary>
    public sealed class SaveLoadMessageAdapter : IInitializable, IDisposable
    {
        private readonly IMessageBroker broker;
        private readonly ISaveService saves;
        private readonly Action<SaveRequested> onSaveRequested;
        private readonly Action<LoadRequested> onLoadRequested;
        private bool subscribed;

        public SaveLoadMessageAdapter(IMessageBroker broker, ISaveService saves)
        {
            this.broker = broker ?? throw new ArgumentNullException(nameof(broker));
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
            onSaveRequested = OnSaveRequested;
            onLoadRequested = OnLoadRequested;
        }

        public void Initialize()
        {
            if (subscribed) return;
            broker.Subscribe(onSaveRequested);
            broker.Subscribe(onLoadRequested);
            subscribed = true;
        }

        public void Dispose()
        {
            if (!subscribed) return;
            broker.Unsubscribe(onSaveRequested);
            broker.Unsubscribe(onLoadRequested);
            subscribed = false;
        }

        private void OnSaveRequested(SaveRequested request)
        {
            string error = null;
            try { saves.Save(request.Slot); }
            catch (Exception e) { error = e.Message; }

            if (error == null) broker.Publish(new GameSaved(request.Slot));
            else broker.Publish(new SaveFailed(request.Slot, error));
        }

        private void OnLoadRequested(LoadRequested request)
        {
            string error = null;
            try
            {
                if (!saves.TryRead(request.Slot, out var data)) error = $"No existe el guardado '{request.Slot}'.";
                else saves.Apply(data);
            }
            catch (Exception e) { error = e.Message; }

            if (error == null) broker.Publish(new GameLoaded(request.Slot));
            else broker.Publish(new LoadFailed(request.Slot, error));
        }
    }
}
