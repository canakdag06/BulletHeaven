using System;
using System.Collections;
using UnityEngine;
using BulletHeaven.Core.Save;

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

        // ── Persistent data ──────────────────────────────────────────────────
        public int TotalEnemiesDefeated { get; private set; }
        public int UnlockedLevelIndex   { get; private set; } = 1;

        // ── Session data ─────────────────────────────────────────────────────
        public int CurrentLevel { get; private set; } = 1;
        public const int MaxLevel = 3;
        public int EnemiesDefeatedThisRun { get; private set; }
        public float RoundTimer { get; set; }

        public event Action<int> OnTimerSecondChanged;

        /// <summary>levelKills, totalKills, isLastLevel</summary>
        public event Action<int, int, bool> OnLevelComplete;

        public event Action OnRoundReset;
        public event Action OnLevelTransitionStarted;
        public event Action OnGameOver;

        public bool IsInputEnabled { get; private set; } = true;

        private ISaveService _saveService;

        private int _lastSecondRecorded;
        private GameObject _currentMapInstance;

        // ── Lifecycle ────────────────────────────────────────────────────────

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

            _saveService = new JsonSaveService();

            GameSaveData saved = _saveService.Load();
            TotalEnemiesDefeated = saved.TotalEnemiesDefeated;
            UnlockedLevelIndex   = saved.UnlockedLevelIndex;
            CurrentLevel         = Mathf.Clamp(saved.CurrentLevelIndex, 1, MaxLevel);
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

        private void OnApplicationQuit()
        {
            PersistSave();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public void ChangeState(IGameState newState) => StateMachine.ChangeState(newState);

        public void SetInputEnabled(bool value) => IsInputEnabled = value;

        public void SetPlayerInvincible(bool value)
        {
            if (PlayerTransform == null) return;
            var health = PlayerTransform.GetComponent<PlayerHealth>();
            health?.SetInvincible(value);
        }

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
                rb.position        = worldPosition;
                rb.linearVelocity  = Vector3.zero;
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

        // ── Level completion ─────────────────────────────────────────────────

        /// <summary>
        /// Called by GameWonState.Enter(). Updates persistent totals, unlocks the next level,
        /// saves to disk, and fires OnLevelComplete for the UI.
        /// </summary>
        public void CompleteLevel()
        {
            TotalEnemiesDefeated += EnemiesDefeatedThisRun;

            if (CurrentLevel >= UnlockedLevelIndex && CurrentLevel < MaxLevel)
                UnlockedLevelIndex = CurrentLevel + 1;

            PersistSave();

            bool isLastLevel = CurrentLevel >= MaxLevel;
            OnLevelComplete?.Invoke(EnemiesDefeatedThisRun, TotalEnemiesDefeated, isLastLevel);
        }

        public void PersistSave()
        {
            _saveService.Save(new GameSaveData
            {
                TotalEnemiesDefeated = TotalEnemiesDefeated,
                UnlockedLevelIndex   = UnlockedLevelIndex,
                CurrentLevelIndex    = CurrentLevel
            });
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
            CurrentLevel = CurrentLevel < MaxLevel ? CurrentLevel + 1 : 1;
            PersistSave();
            StateMachine.ChangeState(new LevelTransitionState(this));
        }

        public void PlayerDied()
        {
            if (StateMachine.CurrentState is not PlayingState) return;
            ChangeState(new GameOverState(this, enemySpawner));
        }

        public void NotifyGameOver() => OnGameOver?.Invoke();

        public void RetryLevel()
        {
            ChangeState(new LevelTransitionState(this));
        }

        public void BeginTransition() => OnLevelTransitionStarted?.Invoke();

        public void FinishTransition()
        {
            StateMachine.ChangeState(new PlayingState(this, enemySpawner));
        }
    }
}
