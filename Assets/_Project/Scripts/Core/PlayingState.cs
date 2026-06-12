
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
            // TODO: Oynanýþ UI'ýný aç, zamanlayýcýyý (timer) baþlat, düþman spawn sistemini tetikle.
            UnityEngine.Debug.Log("GAME STARTED");
        }

        public void Tick()
        {
            // TODO: Timer'ý güncelle. Timer <= 0 olursa GameWonState'e geçiþ yap.
        }

        public void Exit()
        {
            // TODO: Düþmanlarý durdur, game UI'ýný kapat.
        }
    }
}
