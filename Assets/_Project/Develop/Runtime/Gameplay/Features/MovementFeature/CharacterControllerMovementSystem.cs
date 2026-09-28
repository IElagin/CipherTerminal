using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature
{
    public class CharacterControllerMovementSystem : IInitializableSystem, IUpdatableSystem
    {
        private ReactiveVariable<Vector3> _direction;
        private ReactiveVariable<float> _speed;
        private CharacterController _characterController;

        public void OnInit(Entity entity)
        {
            _direction = entity.MoveDirection;
            _speed = entity.MoveSpeed;
            _characterController = entity.CharacterController;
        }

        public void OnUpdate(float deltaTime)
        {
            _characterController.Move(_direction.Value * _speed.Value * deltaTime);
        }
    }
}
