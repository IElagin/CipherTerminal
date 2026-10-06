using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature
{
    public class RigidbodyMovementSystem : IInitializableSystem, IFixedUpdatableSystem
    {
        private ReactiveVariable<Vector3> _direction;
        private ReactiveVariable<float> _speed;
        private Rigidbody _rigidbody;
        private ReactiveVariable<bool> _isMoving;

        private ICompositeCondition _canMove;

        public void OnInit(Entity entity)
        {
            _direction = entity.MoveDirection;
            _speed = entity.MoveSpeed;
            _rigidbody = entity.Rigidbody;
            _isMoving = entity.IsMoving;

            _canMove = entity.CanMove;
        }

        public void OnFixedUpdate(float deltaTime)
        {
            if (_canMove.Evaluate() == false)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _isMoving.Value = false;
                return;
            }

            Vector3 velocity = _direction.Value * _speed.Value;

            _isMoving.Value = velocity.magnitude > 0;
            _rigidbody.linearVelocity = velocity;
        }
    }
}
