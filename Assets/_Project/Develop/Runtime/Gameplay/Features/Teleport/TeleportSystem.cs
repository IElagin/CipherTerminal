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
        private ReactiveEvent<Vector3> _teleportToPointRequest;
        private ReactiveEvent<Vector3> _teleportEvent;

        private ReactiveVariable<Vector3> _center;
        private ReactiveVariable<float> _radius;
        private ReactiveVariable<float> _energyCost;
        private ReactiveVariable<float> _currentEnergy;
        private Rigidbody _rigidbody;

        private ICompositeCondition _canTeleport;

        private IDisposable _requestDisposable;
        private IDisposable _toPointRequestDisposable;

        public void OnInit(Entity entity)
        {
            _teleportRequest = entity.TeleportRequest;
            _teleportToPointRequest = entity.TeleportToPointRequest;
            _teleportEvent = entity.TeleportEvent;

            _center = entity.TeleportCenter;
            _radius = entity.TeleportRadius;
            _energyCost = entity.TeleportEnergyCost;
            _currentEnergy = entity.CurrentEnergy;
            _rigidbody = entity.Rigidbody;

            _canTeleport = entity.CanTeleport;

            _requestDisposable = _teleportRequest.Subscribe(OnTeleportRequest);
            _toPointRequestDisposable = _teleportToPointRequest.Subscribe(OnTeleportToPointRequest);
        }

        public void OnDispose()
        {
            _requestDisposable.Dispose();
            _toPointRequestDisposable.Dispose();
        }

        private void OnTeleportRequest()
        {
            if (CanTeleport() == false)
                return;

            Vector2 offset = Random.insideUnitCircle * _radius.Value;
            Teleport(_center.Value + new Vector3(offset.x, 0, offset.y));
        }

        // The jump towards a point is limited by the same teleport radius, counted from the current position.
        private void OnTeleportToPointRequest(Vector3 point)
        {
            if (CanTeleport() == false)
                return;

            Vector3 position = _rigidbody.position;
            Vector3 offset = new Vector3(point.x - position.x, 0, point.z - position.z);
            Teleport(position + Vector3.ClampMagnitude(offset, _radius.Value));
        }

        private bool CanTeleport()
        {
            if (_canTeleport.Evaluate())
                return true;

            Debug.Log("Телепорт недоступен, энергия: " + _currentEnergy.Value);

            return false;
        }

        private void Teleport(Vector3 position)
        {
            // A kinematic body set only through Rigidbody.position is not synced back to the transform here.
            _rigidbody.position = position;
            _rigidbody.transform.position = position;
            _currentEnergy.Value -= _energyCost.Value;
            _teleportEvent.Invoke(position);
            Debug.Log("Телепорт, энергия: " + _currentEnergy.Value);
        }
    }
}
