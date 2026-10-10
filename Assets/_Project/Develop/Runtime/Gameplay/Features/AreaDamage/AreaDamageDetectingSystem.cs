using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class AreaDamageDetectingSystem : IInitializableSystem, IDisposableSystem
    {
        private ReactiveEvent<Vector3> _teleportEvent;
        private ReactiveVariable<float> _radius;
        private LayerMask _mask;
        private Buffer<Collider> _colliders;
        private ReactiveEvent _collidersDetectedEvent;

        private IDisposable _teleportEventDisposable;

        public void OnInit(Entity entity)
        {
            _teleportEvent = entity.TeleportEvent;
            _radius = entity.AreaDamageRadius;
            _mask = entity.AreaDamageMask;
            _colliders = entity.AreaDamageCollidersBuffer;
            _collidersDetectedEvent = entity.AreaDamageCollidersDetectedEvent;

            _teleportEventDisposable = _teleportEvent.Subscribe(OnTeleport);
        }

        public void OnDispose()
        {
            _teleportEventDisposable.Dispose();
        }

        private void OnTeleport(Vector3 position)
        {
            _colliders.Count = Physics.OverlapSphereNonAlloc(position, _radius.Value, _colliders.Items, _mask);
            _collidersDetectedEvent.Invoke();
        }
    }
}
