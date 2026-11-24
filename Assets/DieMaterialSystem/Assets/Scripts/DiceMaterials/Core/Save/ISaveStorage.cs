namespace DiceMaterials
{
    public interface ISaveStorage
    {
        void Write(string slot, string content);
        bool TryRead(string slot, out string content);
        bool Exists(string slot);
        void Delete(string slot);
    }
}
