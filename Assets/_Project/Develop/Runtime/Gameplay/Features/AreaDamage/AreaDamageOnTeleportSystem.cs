using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Utilities;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class AreaDamageOnTeleportSystem : IInitializableSystem, IDisposableSystem
    {
        private readonly CollidersRegistryService _collidersRegistryService;

        private Entity _entity;

        private ReactiveEvent<Vector3> _teleportEvent;

        private ReactiveVariable<float> _damage;
        private ReactiveVariable<float> _radius;
        private LayerMask _mask;
        private Buffer<Collider> _colliders;

        private IDisposable _teleportEventDisposable;

        public AreaDamageOnTeleportSystem(CollidersRegistryService collidersRegistryService)
        {
            _collidersRegistryService = collidersRegistryService;
        }

        public void OnInit(Entity entity)
        {
            _entity = entity;

            _teleportEvent = entity.TeleportEvent;

            _damage = entity.AreaDamage;
            _radius = entity.AreaDamageRadius;
            _mask = entity.AreaDamageMask;
            _colliders = entity.AreaDamageCollidersBuffer;

            _teleportEventDisposable = _teleportEvent.Subscribe(OnTeleport);
        }

        public void OnDispose()
        {
            _teleportEventDisposable.Dispose();
        }

        private void OnTeleport(Vector3 position)
        {
            _colliders.Count = Physics.OverlapSphereNonAlloc(position, _radius.Value, _colliders.Items, _mask);

            int targetsCount = 0;

            for (int i = 0; i < _colliders.Count; i++)
            {
                Entity target = _collidersRegistryService.GetBy(_colliders.Items[i]);

                if (target == null || target == _entity || target.HasComponent<TakeDamageRequest>() == false)
                    continue;

                target.TakeDamageRequest.Invoke(_damage.Value);
                targetsCount++;
            }

            Debug.Log("Урон по области, целей: " + targetsCount);
        }
    }
}
