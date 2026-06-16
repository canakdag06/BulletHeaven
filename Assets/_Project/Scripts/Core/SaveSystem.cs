using UnityEngine;

namespace BulletHeaven.Core
{
    public static class SaveSystem
    {
        private const string KeyTotalKills   = "TotalKills";
        private const string KeyCurrentLevel = "CurrentLevel";

        public static int TotalKills    => PlayerPrefs.GetInt(KeyTotalKills,   0);
        public static int CurrentLevel  => PlayerPrefs.GetInt(KeyCurrentLevel, 1);

        public static void SaveProgress(int currentLevel, int totalKills)
        {
            PlayerPrefs.SetInt(KeyCurrentLevel, currentLevel);
            PlayerPrefs.SetInt(KeyTotalKills,   totalKills);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(KeyTotalKills);
            PlayerPrefs.DeleteKey(KeyCurrentLevel);
            PlayerPrefs.Save();
        }
    }
}
