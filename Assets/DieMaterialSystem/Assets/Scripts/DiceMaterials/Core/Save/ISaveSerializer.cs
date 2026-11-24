namespace DiceMaterials
{
    public interface ISaveSerializer
    {
        string Serialize(GameSaveData data);
        GameSaveData Deserialize(string content);
    }
}
