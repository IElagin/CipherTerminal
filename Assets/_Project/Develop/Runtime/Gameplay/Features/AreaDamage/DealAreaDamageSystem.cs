using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Utilities;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class DealAreaDamageSystem : IInitializableSystem, IDisposableSystem
    {
        private Buffer<Entity> _targets;
        private ReactiveVariable<float> _damage;
        private ReactiveEvent _targetsDetectedEvent;

        private IDisposable _targetsDetectedEventDisposable;

        public void OnInit(Entity entity)
        {
            _targets = entity.AreaDamageEntitiesBuffer;
            _damage = entity.AreaDamage;
            _targetsDetectedEvent = entity.AreaDamageTargetsDetectedEvent;

            _targetsDetectedEventDisposable = _targetsDetectedEvent.Subscribe(OnTargetsDetected);
        }

        public void OnDispose()
        {
            _targetsDetectedEventDisposable.Dispose();
        }

        private void OnTargetsDetected()
        {
            int damagedCount = 0;

            for (int i = 0; i < _targets.Count; i++)
            {
                Entity target = _targets.Items[i];

                if (target.HasComponent<TakeDamageRequest>() == false)
                    continue;

                target.TakeDamageRequest.Invoke(_damage.Value);
                damagedCount++;
            }

            Debug.Log("Урон по области, целей: " + damagedCount);
        }
    }
}
