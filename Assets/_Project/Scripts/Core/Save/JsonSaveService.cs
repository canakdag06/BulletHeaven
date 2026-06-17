using System.IO;
using UnityEngine;

namespace BulletHeaven.Core.Save
{
    public class JsonSaveService : ISaveService
    {
        private readonly string _filePath;

        public JsonSaveService()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
        }

        public void Save(GameSaveData data)
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(_filePath, json);
        }

        public GameSaveData Load()
        {
            if (!File.Exists(_filePath))
                return new GameSaveData();

            string json = File.ReadAllText(_filePath);
            return JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();
        }
    }
}
