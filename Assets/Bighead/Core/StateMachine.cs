namespace Bighead.Core
{
    public class StateMachine
    {
        private State _currentState;

        public void ChangeState(State state)
        {
            _currentState?.Exit();
            _currentState = state;
            _currentState?.Enter();
        }

        public void Execute()
        {
            _currentState?.Execute();
        }
    }

    public abstract class State
    {
        public abstract void Enter();
        public abstract void Execute();
        public abstract void Exit();
    }
}