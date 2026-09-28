using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature
{
    public class RigidbodyMovementSystem : IInitializableSystem, IFixedUpdatableSystem
    {
        private ReactiveVariable<Vector3> _direction;
        private ReactiveVariable<float> _speed;
        private Rigidbody _rigidbody;

        public void OnInit(Entity entity)
        {
            _direction = entity.MoveDirection;
            _speed = entity.MoveSpeed;
            _rigidbody = entity.Rigidbody;
        }

        public void OnFixedUpdate(float deltaTime)
        {
            _rigidbody.linearVelocity = _direction.Value * _speed.Value;
        }
    }
}
