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
        [Header("Base Enemy Stats")]
        public int baseEnemyHealth = 30;
        public float baseEnemyMoveSpeed = 3.5f;
        public int baseEnemyDamage = 10;

        [Header("Wave Stages")]
        public List<WaveStage> waveStages;

        [Header("Timer")]
        [Min(1f)] public float levelTimerSec = 180f;

        [Header("Environment")]
        public GameObject mapPrefab;
        public Vector3 playerSpawnPoint = Vector3.zero;
    }

    [CreateAssetMenu(menuName = "Levels/LevelConfigSO", fileName = "LevelConfig")]
    public class LevelConfigSO : ScriptableObject
    {
        public List<LevelData> levels;

        public LevelData GetLevel(int levelIndex)
        {
            int i = levelIndex - 1;
            if (i >= 0 && i < levels.Count)
                return levels[i];

            Debug.LogError($"[LevelConfigSO] Level {levelIndex} not found.");
            return null;
        }
    }
}
