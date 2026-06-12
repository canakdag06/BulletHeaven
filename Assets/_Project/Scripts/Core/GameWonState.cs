
namespace BulletHeaven.Core
{
    public class GameWonState : IGameState
    {
        private readonly GameManager _gameManager;

        public GameWonState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            // TODO: Game Won UI'ýný aç, öldürülen düþman sayýsýný göster ve save al.
            UnityEngine.Debug.Log("Süre doldu! Game Won State aktif.");
        }

        public void Tick()
        {

        }

        public void Exit()
        {
            // TODO: Clear UI vs.
        }
    }
}
