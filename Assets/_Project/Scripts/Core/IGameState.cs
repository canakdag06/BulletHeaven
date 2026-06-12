
namespace BulletHeaven.Core
{
    public interface IGameState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}