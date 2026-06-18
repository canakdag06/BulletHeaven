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
            _gameManager.SetInputEnabled(false);
            _gameManager.SetPlayerInvincible(true);
            _spawner?.StopSpawning();
            _spawner?.FreezeAllEnemies();
            _gameManager.CompleteLevel();
            Debug.Log($"[GameWonState] Level {_gameManager.CurrentLevel} tamamlandı — " +
                      $"Level kill: {_gameManager.EnemiesDefeatedThisRun}, Toplam kill: {_gameManager.TotalEnemiesDefeated}");
        }

        public void Tick() { }

        public void Exit() { }
    }
}
