using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.StateMachineCore;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI.States
{
    public class AttackTriggerState : State, IUpdatableState
    {
        private ReactiveEvent _attackRequest;

        public AttackTriggerState(Entity entity)
        {
            _attackRequest = entity.StartAttackRequest;
        }

        public override void Enter()
        {
            base.Enter();

            _attackRequest.Invoke();
        }

        public void Update(float deltaTime)
        {
        }
    }
}
