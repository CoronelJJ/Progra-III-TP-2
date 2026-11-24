using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceMaterials
{
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveContributor[] contributors;
        private readonly ISaveSerializer serializer;
        private readonly ISaveStorage storage;

        public SaveService(IEnumerable<ISaveContributor> contributors, ISaveSerializer serializer, ISaveStorage storage)
        {
            this.contributors = (contributors ?? throw new ArgumentNullException(nameof(contributors))).ToArray();
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool Exists(string slot) => storage.Exists(slot);

        public void Save(string slot)
        {
            var data = new GameSaveData();
            foreach (var c in contributors) c.Capture(data);
            storage.Write(slot, serializer.Serialize(data));
        }

        public bool TryRead(string slot, out GameSaveData data)
        {
            data = null;
            if (!storage.TryRead(slot, out var content)) return false;

            data = serializer.Deserialize(content)
                   ?? throw new InvalidOperationException($"El guardado '{slot}' está vacío o es ilegible.");
            if (data.version > GameSaveData.CurrentVersion)
                throw new InvalidOperationException(
                    $"El guardado '{slot}' es de una versión más nueva ({data.version}) que la soportada ({GameSaveData.CurrentVersion}).");
            return true;
        }

        public void Apply(GameSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            foreach (var c in contributors) c.Restore(data);
        }

        public void Delete(string slot) => storage.Delete(slot);
    }
}
