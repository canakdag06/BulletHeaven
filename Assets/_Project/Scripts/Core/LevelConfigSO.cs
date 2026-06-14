using UnityEngine;

using System.Collections.Generic;
using UnityEngine;

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

    [CreateAssetMenu(menuName = "Levels/LevelConfigSO", fileName = "LevelConfig_L_New")]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Level Info")]
        public int levelIndex = 1;

        [Header("Base Enemy Stats")]
        public int baseEnemyHealth = 30;
        public float baseEnemyMoveSpeed = 3.5f;
        public int baseEnemyDamage = 10;

        [Header("Wave Stages")]
        public List<WaveStage> waveStages;

        [Header("Environment")]
        public GameObject mapPrefab;
    }
}
