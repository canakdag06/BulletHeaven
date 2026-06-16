namespace BulletHeaven.Core
{
    /// <summary>
    /// Intermediate state between GameWonState and the next PlayingState.
    /// Delegates the visual transition sequence to LevelTransitionPanel via an event,
    /// then waits — PlayingState is entered by GameManager.FinishTransition().
    /// </summary>
    public class LevelTransitionState : IGameState
    {
        private readonly GameManager _gameManager;

        public LevelTransitionState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter() => _gameManager.BeginTransition();

        public void Tick() { }

        public void Exit() { }
    }
}
