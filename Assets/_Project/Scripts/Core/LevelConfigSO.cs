using UnityEngine;
using System.Collections.Generic;
using BulletHeaven.Enemy;

namespace BulletHeaven.Core
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public EnemyDefinitionSO enemy;
        [Min(0)] public int weight = 1;
    }

    [System.Serializable]
    public class WaveStage
    {
        public float startTime;
        public float spawnInterval;
        public int maxEnemiesAlive;
        public int enemiesPerSpawn;
        [Min(0.1f)] public float healthMultiplier = 1f;
        [Min(0.1f)] public float speedMultiplier = 1f;

        [Tooltip("Enemies that can spawn during this stage, picked by weight.")]
        public List<EnemySpawnEntry> enemies = new();

        public EnemyDefinitionSO PickEnemy()
        {
            if (enemies == null) return null;

            int totalWeight = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].enemy != null)
                    totalWeight += enemies[i].weight;
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemySpawnEntry entry = enemies[i];
                if (entry.enemy == null) continue;

                roll -= entry.weight;
                if (roll < 0) return entry.enemy;
            }

            return null;
        }
    }

    [System.Serializable]
    public class LevelData
    {
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
