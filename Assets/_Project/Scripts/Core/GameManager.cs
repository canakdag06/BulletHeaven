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
        public float RoundTimer { get; set; } = 180f;   // 3 minutes

        public event Action<int> OnTimerSecondChanged;

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

        // ── Round lifecycle ──────────────────────────────────────────────────

        public void ResetRoundStats()
        {
            RoundTimer = 180f;
            _lastSecondRecorded = 180;
            EnemiesDefeatedThisRun = 0;
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
