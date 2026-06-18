namespace BulletHeaven.Enemy
{
    public interface IEnemyState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}
