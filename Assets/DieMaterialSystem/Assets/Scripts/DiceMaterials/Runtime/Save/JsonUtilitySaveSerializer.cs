using UnityEngine;

namespace DiceMaterials.Runtime
{
    public sealed class JsonUtilitySaveSerializer : ISaveSerializer
    {
        private readonly bool prettyPrint;

        public JsonUtilitySaveSerializer(bool prettyPrint = false) { this.prettyPrint = prettyPrint; }

        public string Serialize(GameSaveData data) => JsonUtility.ToJson(data, prettyPrint);

        public GameSaveData Deserialize(string content) => JsonUtility.FromJson<GameSaveData>(content);
    }
}
