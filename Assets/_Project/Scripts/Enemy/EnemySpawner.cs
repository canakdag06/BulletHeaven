using System.Collections;
using UnityEngine;
using BulletHeaven.Core;
using BulletHeaven.Enemy;

public class EnemySpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private LevelConfigSO levelConfig;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 20f;

    private Transform _playerTransform;
    private Camera    _mainCamera;
    private WaveStage _currentStage;
    private int       _activeEnemyCount;
    private int       _currentStageIndex;
    private float     _elapsedTime;
    private bool      _isRunning;
    private Coroutine _spawnCoroutine;


    public void StartSpawning(LevelConfigSO config)
    {
        if (config == null || config.waveStages == null || config.waveStages.Count == 0)
        {
            Debug.LogError("[EnemySpawner] LevelConfig is missing or has no wave stages.");
            return;
        }

        levelConfig       = config;
        _elapsedTime      = 0f;
        _currentStageIndex = 0;
        _activeEnemyCount = 0;
        _isRunning        = true;

        _playerTransform = GameObject.FindWithTag("Player")?.transform;
        _mainCamera      = Camera.main;

        if (_playerTransform == null)
            Debug.LogWarning("[EnemySpawner] Player not found.");

        ApplyStage(0);
    }

    public void StopSpawning()
    {
        _isRunning = false;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    public void OnEnemyRemoved() => _activeEnemyCount--;

    // -- Wave Tracking --

    private void Update()
    {
        if (!_isRunning) return;

        _elapsedTime += Time.deltaTime;
        CheckWaveProgression();
    }

    private void CheckWaveProgression()
    {
        if (levelConfig.waveStages == null) return;

        // pick the last stage whose startTime <= elapsedTime.
        for (int i = levelConfig.waveStages.Count - 1; i >= 0; i--)
        {
            if (_elapsedTime >= levelConfig.waveStages[i].startTime)
            {
                if (i != _currentStageIndex)
                    ApplyStage(i);
                break;
            }
        }
    }

    private void ApplyStage(int index)
    {
        _currentStageIndex = index;
        _currentStage      = levelConfig.waveStages[index];

        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);

        _spawnCoroutine = StartCoroutine(SpawnRoutine());

        Debug.Log($"[EnemySpawner] Stage {index} activated. Interval: {_currentStage.spawnInterval}s");
    }

    // Spawn Loop

    private IEnumerator SpawnRoutine()
    {
        while (_isRunning)
        {
            yield return new WaitForSeconds(_currentStage.spawnInterval);

            if (_activeEnemyCount < _currentStage.maxEnemiesAlive)
                SpawnBatch();
        }
    }

    private void SpawnBatch()
    {
        int toSpawn = Mathf.Min(
            _currentStage.enemiesPerSpawn,
            _currentStage.maxEnemiesAlive - _activeEnemyCount
        );

        for (int i = 0; i < toSpawn; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        Vector3 spawnPos = GetOffscreenSpawnPoint();
        if (spawnPos == Vector3.zero) return;

        Enemy enemy = PoolManager.Instance.Get(EPoolType.Enemy) as Enemy;
        if (enemy == null) return;

        enemy.transform.position = spawnPos;
        enemy.transform.rotation = Quaternion.identity;

        int   health = Mathf.RoundToInt(levelConfig.baseEnemyHealth * _currentStage.healthMultiplier);
        float speed  = levelConfig.baseEnemyMoveSpeed * _currentStage.speedMultiplier;
        int   dmg    = levelConfig.baseEnemyDamage;

        enemy.Configure(health, speed, dmg);
        enemy.OnEnemyRemoved += OnEnemyRemoved;

        _activeEnemyCount++;
    }

    // Spawn Position 

    /// <summary>
    /// Returns a random point on the spawn circle that falls outside
    /// the camera viewport so enemies are never seen popping in.
    /// </summary>
    private Vector3 GetOffscreenSpawnPoint()
    {
        if (_playerTransform == null) return Vector3.zero;

        if (_mainCamera == null)
            return RandomCirclePoint();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 candidate = RandomCirclePoint();
            Vector3 viewport  = _mainCamera.WorldToViewportPoint(candidate);

            bool outsideX = viewport.x < 0f || viewport.x > 1f;
            bool outsideY = viewport.y < 0f || viewport.y > 1f;
            bool inFront  = viewport.z > 0f;

            if (inFront && (outsideX || outsideY))
                return candidate;
        }

        return RandomCirclePoint();
    }

    private Vector3 RandomCirclePoint()
    {
        float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
        return _playerTransform.position + offset;
    }

    // -- Gizmos --

    private void OnDrawGizmosSelected()
    {
        if (_playerTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_playerTransform.position, spawnRadius);
    }
}
