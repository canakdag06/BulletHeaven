using UnityEngine;

namespace BulletHeaven.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameStateMachine StateMachine { get; private set; }

        // States
        public PlayingState PlayingState { get; private set; }
        public GameWonState GameWonState { get; private set; }
        public LevelTransitionState LevelTransitionState { get; private set; }

        // Level tracking
        public int CurrentLevel { get; private set; } = 1;
        public const int MaxLevel = 3;

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
            PlayingState = new PlayingState(this);
            GameWonState = new GameWonState(this);
            LevelTransitionState = new LevelTransitionState(this);
        }

        private void Start()
        {
            StateMachine.Initialize(PlayingState);
        }

        private void Update()
        {
            StateMachine.Tick();
        }

        // Player
        public Transform PlayerTransform { get; private set; }

        public void RegisterPlayer(Transform playerTransform)
        {
            PlayerTransform = playerTransform;
        }

        // Enemy tracking
        public int EnemiesDefeated { get; private set; }

        public void OnEnemyDefeated()
        {
            EnemiesDefeated++;
        }

        public void GoToNextLevel()
        {
            if (CurrentLevel < MaxLevel)
            {
                CurrentLevel++;
                StateMachine.ChangeState(LevelTransitionState);
            }
        }
    }
}
