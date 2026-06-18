namespace BulletHeaven.Core
{
    public class GameOverState : IGameState
    {
        private readonly GameManager  _gameManager;
        private readonly EnemySpawner _spawner;

        public GameOverState(GameManager gameManager, EnemySpawner spawner)
        {
            _gameManager = gameManager;
            _spawner     = spawner;
        }

        public void Enter()
        {
            _gameManager.SetInputEnabled(false);
            _spawner?.StopSpawning();
            _gameManager.NotifyGameOver();
        }

        public void Tick() { }

        public void Exit() { }
    }
}
