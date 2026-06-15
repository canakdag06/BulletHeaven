
namespace BulletHeaven.Core
{
    public class PlayingState : IGameState
    {
        private readonly GameManager _gameManager;

        public PlayingState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            _gameManager.EnemySpawner?.StartSpawning();
        }

        public void Tick()
        {
            // TODO: Timer'? g�ncelle. Timer <= 0 olursa GameWonState'e ge�i? yap.
        }

        public void Exit()
        {
            _gameManager.EnemySpawner?.StopSpawning();
        }
    }
}
