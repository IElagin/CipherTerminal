using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature
{
    public class CharacterControllerMovementSystem : IInitializableSystem, IUpdatableSystem
    {
        private ReactiveVariable<Vector3> _direction;
        private ReactiveVariable<float> _speed;
        private CharacterController _characterController;
        private ReactiveVariable<bool> _isMoving;

        private ICompositeCondition _canMove;

        public void OnInit(Entity entity)
        {
            _direction = entity.MoveDirection;
            _speed = entity.MoveSpeed;
            _characterController = entity.CharacterController;
            _isMoving = entity.IsMoving;

            _canMove = entity.CanMove;
        }

        public void OnUpdate(float deltaTime)
        {
            if (_canMove.Evaluate() == false)
            {
                _isMoving.Value = false;
                return;
            }

            Vector3 velocity = _direction.Value * _speed.Value;

            _isMoving.Value = velocity.magnitude > 0;
            _characterController.Move(velocity * deltaTime);
        }
    }
}
