using UnityEngine;
using System.Collections.Generic;

namespace BulletHeaven.Core
{
    [System.Serializable]
    public class WaveStage
    {
        public float startTime;
        public float spawnInterval;
        public int maxEnemiesAlive;
        public int enemiesPerSpawn;
        [Min(0.1f)] public float healthMultiplier = 1f;
        [Min(0.1f)] public float speedMultiplier = 1f;
    }

    [System.Serializable]
    public class LevelData
    {
        public string levelName;

        [Header("Base Enemy Stats")]
        public int baseEnemyHealth = 30;
        public float baseEnemyMoveSpeed = 3.5f;
        public int baseEnemyDamage = 10;

        [Header("Wave Stages")]
        public List<WaveStage> waveStages;

        [Header("Environment")]
        public GameObject mapPrefab;
    }

    [CreateAssetMenu(menuName = "BulletHeaven/GameConfigSO", fileName = "GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        public List<LevelData> levels;

        public LevelData GetLevel(int levelIndex)
        {
            int i = levelIndex - 1;
            if (i >= 0 && i < levels.Count)
                return levels[i];

            Debug.LogError($"[GameConfigSO] Level {levelIndex} not found.");
            return null;
        }
    }
}
