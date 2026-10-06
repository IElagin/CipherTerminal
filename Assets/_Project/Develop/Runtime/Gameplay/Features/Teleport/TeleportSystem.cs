using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Teleport
{
    public class TeleportSystem : IInitializableSystem, IDisposableSystem
    {
        private ReactiveEvent _teleportRequest;
        private ReactiveEvent<Vector3> _teleportEvent;

        private ReactiveVariable<float> _radius;
        private ReactiveVariable<float> _energyCost;
        private ReactiveVariable<float> _currentEnergy;
        private Rigidbody _rigidbody;

        private ICompositeCondition _canTeleport;

        private IDisposable _requestDisposable;

        public void OnInit(Entity entity)
        {
            _teleportRequest = entity.TeleportRequest;
            _teleportEvent = entity.TeleportEvent;

            _radius = entity.TeleportRadius;
            _energyCost = entity.TeleportEnergyCost;
            _currentEnergy = entity.CurrentEnergy;
            _rigidbody = entity.Rigidbody;

            _canTeleport = entity.CanTeleport;

            _requestDisposable = _teleportRequest.Subscribe(OnTeleportRequest);
        }

        public void OnDispose()
        {
            _requestDisposable.Dispose();
        }

        private void OnTeleportRequest()
        {
            if (_canTeleport.Evaluate() == false)
            {
                Debug.Log("Телепорт недоступен, энергия: " + _currentEnergy.Value);
                return;
            }

            Vector2 offset = Random.insideUnitCircle * _radius.Value;
            Vector3 position = _rigidbody.position + new Vector3(offset.x, 0, offset.y);

            _rigidbody.position = position;
            _currentEnergy.Value -= _energyCost.Value;
            _teleportEvent.Invoke(position);
            Debug.Log("Телепорт, энергия: " + _currentEnergy.Value);
        }
    }
}
