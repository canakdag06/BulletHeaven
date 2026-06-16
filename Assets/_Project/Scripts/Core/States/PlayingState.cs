using UnityEngine;

namespace BulletHeaven.Core
{
    public class PlayingState : IGameState
    {
        private readonly GameManager _gameManager;
        private readonly EnemySpawner _spawner;

        // Guard: prevents a second ChangeState call if Tick fires again in the same frame
        private bool _hasEnded;

        public PlayingState(GameManager gameManager, EnemySpawner spawner)
        {
            _gameManager = gameManager;
            _spawner = spawner;
        }

        public void Enter()
        {
            _hasEnded = false;
            _gameManager.SetInputEnabled(true);
            _gameManager.ResetRoundStats();
            _spawner?.StartSpawning();
        }

        public void Tick()
        {
            if (_hasEnded) return;

            _gameManager.RoundTimer -= Time.deltaTime;

            // CeilToInt: 179.9 → 180, so the display shows 180 at the start and reaches 0 at expiry
            int secondsLeft = Mathf.CeilToInt(_gameManager.RoundTimer);
            _gameManager.EvaluateTimerChange(secondsLeft);

            if (_gameManager.RoundTimer <= 0f)
            {
                _gameManager.RoundTimer = 0f;
                _hasEnded = true;
                _gameManager.ChangeState(new GameWonState(_gameManager, _spawner));
            }
        }

        public void Exit()
        {
            _spawner?.StopSpawning();
        }
    }
}
