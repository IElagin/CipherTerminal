using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature
{
    public class RigidbodyRotationSystem : IInitializableSystem, IFixedUpdatableSystem
    {
        private ReactiveVariable<Vector3> _direction;
        private ReactiveVariable<float> _speed;
        private Rigidbody _rigidbody;

        public void OnInit(Entity entity)
        {
            _direction = entity.RotationDirection;
            _speed = entity.RotationSpeed;
            _rigidbody = entity.Rigidbody;
        }

        public void OnFixedUpdate(float deltaTime)
        {
            Vector3 direction = _direction.Value;

            if (direction == Vector3.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion rotation = Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, _speed.Value * deltaTime);
            _rigidbody.MoveRotation(rotation);
        }
    }
}
