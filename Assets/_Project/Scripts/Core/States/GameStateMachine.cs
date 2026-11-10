using System.Collections.Generic;

namespace BulletHeaven.Core
{
    public class GameStateMachine
    {
        public IGameState CurrentState { get; private set; }

        private readonly Stack<IGameState> _stack = new();
        private bool _isPopping;

        public void Initialize(IGameState startingState) => Replace(startingState);

        public void ChangeState(IGameState newState) => Replace(newState);

        /// <summary>
        /// Runs an overlay on top of the current state.
        /// The state underneath is not exited, so <see cref="PlayingState"/> keeps its round and spawner.
        /// </summary>
        public void PushState(IGameState state)
        {
            if (state == null) return;

            _stack.Push(CurrentState);
            CurrentState = state;
            CurrentState.Enter();
        }

        /// <summary>
        /// Leaves the overlay. The revealed state is not entered again.
        /// </summary>
        public void PopState()
        {
            if (_isPopping || CurrentState == null) return;

            _isPopping = true;
            CurrentState.Exit();
            CurrentState = _stack.Count > 0 ? _stack.Pop() : null;
            _isPopping = false;
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }

        // Exit the current state, then every state still on the stack, then enter the replacement.
        private void Replace(IGameState newState)
        {
            CurrentState?.Exit();
            CurrentState = null;

            while (_stack.Count > 0)
                _stack.Pop()?.Exit();

            CurrentState = newState;
            CurrentState?.Enter();
        }
    }
}
