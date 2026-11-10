using UnityEngine;

namespace BulletHeaven.Core
{
    /// <summary>
    /// Overlay on <see cref="PlayingState"/>. Pauses the world without exiting the round.
    /// </summary>
    public class LevelUpState : IGameState
    {
        private readonly GameManager _gameManager;

        public LevelUpState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            Time.timeScale = 0f;
            _gameManager.SetInputEnabled(false);
            _gameManager.RaiseOffers();
        }

        public void Tick() { }

        public void Exit()
        {
            Time.timeScale = 1f;
            _gameManager.SetInputEnabled(true);
            _gameManager.CloseLevelUp();
        }
    }
}
