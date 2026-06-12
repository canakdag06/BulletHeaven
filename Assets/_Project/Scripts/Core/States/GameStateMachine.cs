
namespace BulletHeaven.Core
{
    public class GameStateMachine
    {
        public IGameState CurrentState { get; private set; }

        public void Initialize(IGameState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void ChangeState(IGameState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }
    }
}