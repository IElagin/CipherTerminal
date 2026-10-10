using System.Collections.Generic;

namespace Assets._Project.Develop.Runtime.Utilities.StateMachineCore
{
    public abstract class ParallelState<TState> : State where TState : class, IState
    {
        private List<TState> _states;

        public ParallelState(params TState[] states)
        {
            _states = new List<TState>(states);
        }

        protected IReadOnlyList<TState> States => _states;

        public override void Enter()
        {
            base.Enter();

            foreach (TState state in _states)
                state.Enter();
        }

        public override void Exit()
        {
            base.Exit();

            foreach (TState state in _states)
                state.Exit();
        }
    }
}
