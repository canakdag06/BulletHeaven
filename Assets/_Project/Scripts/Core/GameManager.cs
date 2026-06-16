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

        // Level tracking
        public int CurrentLevel { get; private set; } = 1;
        public const int MaxLevel = 3;
        public int EnemiesDefeatedThisRun { get; private set; }
        public int TotalKillsAllTime { get; private set; }
        public float RoundTimer { get; set; } = 60f;   // 1 minute

        public event Action<int> OnTimerSecondChanged;

        /// <summary>levelKills, totalKills, isLastLevel</summary>
        public event Action<int, int, bool> OnLevelComplete;

        public event Action OnRoundReset;

        private int _lastSecondRecorded;

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
            RoundTimer = 60f;
            _lastSecondRecorded = 60;
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

        // ── Level flow ───────────────────────────────────────────────────────
        public void GoToNextLevel()
        {
            if (CurrentLevel < MaxLevel)
            {
                CurrentLevel++;
                StateMachine.ChangeState(new LevelTransitionState(this));
            }
        }
    }
}
