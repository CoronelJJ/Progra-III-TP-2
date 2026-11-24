using System.Collections.Generic;

namespace DiceMaterials.Tests
{
    public sealed class FakeSaveStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public void Write(string slot, string content) => Files[slot] = content;
        public bool TryRead(string slot, out string content) => Files.TryGetValue(slot, out content);
        public bool Exists(string slot) => Files.ContainsKey(slot);
        public void Delete(string slot) => Files.Remove(slot);
    }
}
