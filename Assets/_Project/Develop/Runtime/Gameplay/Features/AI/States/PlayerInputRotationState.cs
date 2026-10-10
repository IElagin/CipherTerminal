using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.StateMachineCore;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI.States
{
    public class PlayerInputRotationState : State, IUpdatableState
    {
        private IInputService _inputService;
        private ReactiveVariable<Vector3> _rotationDirection;
        private Transform _transform;

        public PlayerInputRotationState(
            Entity entity,
            IInputService inputService)
        {
            _inputService = inputService;
            _rotationDirection = entity.RotationDirection;
            _transform = entity.Transform;
        }

        public void Update(float deltaTime)
        {
            float rotationDelta = _inputService.RotationDelta;

            if (rotationDelta == 0)
                return;

            Vector3 direction = _rotationDirection.Value == Vector3.zero ? _transform.forward : _rotationDirection.Value;
            _rotationDirection.Value = Quaternion.Euler(0, rotationDelta, 0) * direction;
        }
    }
}
