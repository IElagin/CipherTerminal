using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature
{
    public class TransformRotationSystem : IInitializableSystem, IUpdatableSystem
    {
        private ReactiveVariable<Vector3> _direction;
        private ReactiveVariable<float> _speed;
        private Transform _transform;

        public void OnInit(Entity entity)
        {
            _direction = entity.RotationDirection;
            _speed = entity.RotationSpeed;
            _transform = entity.Transform;
        }

        public void OnUpdate(float deltaTime)
        {
            Vector3 direction = _direction.Value;

            if (direction == Vector3.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation, _speed.Value * deltaTime);
            _transform.rotation = rotation;
        }
    }
}
