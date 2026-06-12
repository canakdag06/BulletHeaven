using UnityEngine;
using UnityEngine.SceneManagement;

namespace BulletHeaven.Core
{
    public class LevelTransitionState : IGameState
    {
        private readonly GameManager _gameManager;

        public LevelTransitionState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            //string sceneName = "Level" + _gameManager.CurrentLevel;
            //Debug.Log($"Loading {sceneName}...");
            //SceneManager.LoadScene(sceneName);
        }

        public void Tick() { }

        public void Exit() { }
    }
}
