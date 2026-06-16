using UnityEngine;

namespace BulletHeaven.Core
{
    public class GameWonState : IGameState
    {
        private readonly GameManager _gameManager;

        // Reserved for future pool cleanup
        private readonly EnemySpawner _spawner;

        public GameWonState(GameManager gameManager, EnemySpawner spawner)
        {
            _gameManager = gameManager;
            _spawner = spawner;
        }

        public void Enter()
        {
            Debug.Log($"[GameWonState] Game Won! Time is up. Enemies defeated this run: {_gameManager.EnemiesDefeatedThisRun}");
        }

        public void Tick() { }

        public void Exit() { }
    }
}
