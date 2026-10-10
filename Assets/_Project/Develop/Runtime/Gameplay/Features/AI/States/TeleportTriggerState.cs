using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.StateMachineCore;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI.States
{
    public class TeleportTriggerState : State, IUpdatableState
    {
        private ReactiveEvent _teleportRequest;

        public TeleportTriggerState(Entity entity)
        {
            _teleportRequest = entity.TeleportRequest;
        }

        public override void Enter()
        {
            base.Enter();

            _teleportRequest.Invoke();
        }

        public void Update(float deltaTime)
        {
        }
    }
}
