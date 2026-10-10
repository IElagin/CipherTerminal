using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class AreaDamageEntitiesFilterSystem : IInitializableSystem, IDisposableSystem
    {
        private readonly CollidersRegistryService _collidersRegistryService;

        private Entity _entity;
        private Buffer<Collider> _colliders;
        private Buffer<Entity> _targets;
        private ReactiveEvent _collidersDetectedEvent;
        private ReactiveEvent _targetsDetectedEvent;

        private IDisposable _collidersDetectedEventDisposable;

        public AreaDamageEntitiesFilterSystem(CollidersRegistryService collidersRegistryService)
        {
            _collidersRegistryService = collidersRegistryService;
        }

        public void OnInit(Entity entity)
        {
            _entity = entity;
            _colliders = entity.AreaDamageCollidersBuffer;
            _targets = entity.AreaDamageEntitiesBuffer;
            _collidersDetectedEvent = entity.AreaDamageCollidersDetectedEvent;
            _targetsDetectedEvent = entity.AreaDamageTargetsDetectedEvent;

            _collidersDetectedEventDisposable = _collidersDetectedEvent.Subscribe(OnCollidersDetected);
        }

        public void OnDispose()
        {
            _collidersDetectedEventDisposable.Dispose();
        }

        private void OnCollidersDetected()
        {
            _targets.Count = 0;

            for (int i = 0; i < _colliders.Count; i++)
            {
                Entity target = _collidersRegistryService.GetBy(_colliders.Items[i]);

                if (target == null || target == _entity || ContainsInTargets(target))
                    continue;

                _targets.Items[_targets.Count] = target;
                _targets.Count++;
            }

            _targetsDetectedEvent.Invoke();
        }

        private bool ContainsInTargets(Entity entity)
        {
            for (int i = 0; i < _targets.Count; i++)
                if (_targets.Items[i] == entity)
                    return true;

            return false;
        }
    }
}
