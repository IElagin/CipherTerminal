using Assets._Project.Develop.Runtime.Utilities.Reactive;

namespace Assets._Project.Develop.Runtime.Utilities.StateMachineCore
{
    public interface IState
    {
        IReadOnlyEvent Entered { get; }
        IReadOnlyEvent Exited { get; }

        void Enter();
        void Exit();
    }
}
