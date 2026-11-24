namespace DiceMaterials
{
    public interface ISaveService
    {
        bool Exists(string slot);
        void Save(string slot);

        /// <summary>Lee y valida el guardado SIN aplicarlo (sirve para conocer la seed antes de crear la partida).</summary>
        bool TryRead(string slot, out GameSaveData data);

        /// <summary>Aplica un guardado ya leído a los sistemas vivos.</summary>
        void Apply(GameSaveData data);

        void Delete(string slot);
    }
}
