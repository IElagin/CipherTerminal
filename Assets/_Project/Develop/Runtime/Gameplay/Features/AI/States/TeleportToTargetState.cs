using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.StateMachineCore;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI.States
{
    public class TeleportToTargetState : State, IUpdatableState
    {
        private ReactiveEvent<Vector3> _teleportToPointRequest;
        private ReactiveVariable<Entity> _currentTarget;
        private Transform _transform;

        private float _distanceToTarget;

        public TeleportToTargetState(Entity entity, float distanceToTarget)
        {
            _teleportToPointRequest = entity.TeleportToPointRequest;
            _currentTarget = entity.CurrentTarget;
            _transform = entity.Transform;

            _distanceToTarget = distanceToTarget;
        }

        public override void Enter()
        {
            base.Enter();

            Entity target = _currentTarget.Value;

            if (target == null)
                return;

            Vector3 targetPosition = target.Transform.position;
            Vector3 fromTarget = _transform.position - targetPosition;
            fromTarget.y = 0;

            _teleportToPointRequest.Invoke(targetPosition + fromTarget.normalized * _distanceToTarget);
        }

        public void Update(float deltaTime)
        {
        }
    }
}
