using System;
using System.Collections;
using UnityEngine;

namespace BulletHeaven.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameStateMachine StateMachine { get; private set; }

        [SerializeField] private EnemySpawner enemySpawner;
        public EnemySpawner EnemySpawner => enemySpawner;

        [SerializeField] private LevelConfigSO levelConfig;
        public LevelConfigSO LevelConfig => levelConfig;

        // Level tracking
        public int CurrentLevel { get; private set; } = 1;
        public const int MaxLevel = 3;
        public int EnemiesDefeatedThisRun { get; private set; }
        public int TotalKillsAllTime { get; private set; }
        public float RoundTimer { get; set; }

        public event Action<int> OnTimerSecondChanged;

        /// <summary>levelKills, totalKills, isLastLevel</summary>
        public event Action<int, int, bool> OnLevelComplete;

        public event Action OnRoundReset;

        public event Action OnLevelTransitionStarted;

        private int _lastSecondRecorded;
        private GameObject _currentMapInstance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StateMachine = new GameStateMachine();
            TotalKillsAllTime = SaveSystem.TotalKills;
        }

        private void Start()
        {
            if (enemySpawner == null)
                enemySpawner = FindFirstObjectByType<EnemySpawner>();

            StartCoroutine(BeginGame());
        }

        // Waits one frame so PlayerController.RegisterPlayer (Start) runs before the first state enters
        private IEnumerator BeginGame()
        {
            yield return null;
            SpawnMap();
            StateMachine.Initialize(new PlayingState(this, enemySpawner));
        }

        private void Update()
        {
            StateMachine.Tick();
        }

        // wrapper to keep states decoupled from StateMachine internals
        public void ChangeState(IGameState newState) => StateMachine.ChangeState(newState);

        // ── Player ──────────────────────────────────────────────────────────
        public Transform PlayerTransform { get; private set; }

        public void RegisterPlayer(Transform playerTransform)
        {
            PlayerTransform = playerTransform;
        }

        /// <summary>Teleports the player's Rigidbody to the given world position and zeroes its velocity.</summary>
        public void SetPlayerPosition(Vector3 worldPosition)
        {
            if (PlayerTransform == null) return;

            var rb = PlayerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = worldPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                PlayerTransform.position = worldPosition;
            }
        }

        // ── Enemy tracking ───────────────────────────────────────────────────
        public void OnEnemyDefeated()
        {
            EnemiesDefeatedThisRun++;
        }

        public void CompleteLevel()
        {
            TotalKillsAllTime += EnemiesDefeatedThisRun;
            SaveSystem.SaveProgress(CurrentLevel, TotalKillsAllTime);
            bool isLastLevel = CurrentLevel >= MaxLevel;
            OnLevelComplete?.Invoke(EnemiesDefeatedThisRun, TotalKillsAllTime, isLastLevel);
        }

        // ── Round lifecycle ──────────────────────────────────────────────────

        public void ResetRoundStats()
        {
            LevelData data = levelConfig != null ? levelConfig.GetLevel(CurrentLevel) : null;
            float timer = data != null ? data.levelTimerSec : 180f;

            RoundTimer = timer;
            _lastSecondRecorded = Mathf.CeilToInt(timer);
            EnemiesDefeatedThisRun = 0;
            OnRoundReset?.Invoke();
        }

        // Called every Tick from PlayingState; fires OnTimerSecondChanged only on a new second
        public void EvaluateTimerChange(int currentSeconds)
        {
            if (currentSeconds == _lastSecondRecorded) return;

            _lastSecondRecorded = currentSeconds;
            OnTimerSecondChanged?.Invoke(currentSeconds);
        }

        // ── Map management ───────────────────────────────────────────────────

        /// <summary>Destroys the current map instance and instantiates the one for CurrentLevel.</summary>
        public void SpawnMap()
        {
            if (_currentMapInstance != null)
            {
                Destroy(_currentMapInstance);
                _currentMapInstance = null;
            }

            LevelData data = levelConfig != null ? levelConfig.GetLevel(CurrentLevel) : null;
            if (data?.mapPrefab != null)
                _currentMapInstance = Instantiate(data.mapPrefab);
        }

        // ── Level flow ───────────────────────────────────────────────────────

        public void GoToNextLevel()
        {
            if (CurrentLevel < MaxLevel)
            {
                CurrentLevel++;
                StateMachine.ChangeState(new LevelTransitionState(this));
            }
        }

        /// <summary>Called by LevelTransitionState to kick off the visual transition sequence.</summary>
        public void BeginTransition() => OnLevelTransitionStarted?.Invoke();

        /// <summary>Called by LevelTransitionPanel once the fade-out completes and the map is ready.</summary>
        public void FinishTransition()
        {
            StateMachine.ChangeState(new PlayingState(this, enemySpawner));
        }
    }
}
