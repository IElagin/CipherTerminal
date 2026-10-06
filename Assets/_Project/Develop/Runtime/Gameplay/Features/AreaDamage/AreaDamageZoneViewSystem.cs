using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class AreaDamageZoneViewSystem : IInitializableSystem, IUpdatableSystem, IDisposableSystem
    {
        private GameObject _zone;
        private ReactiveVariable<float> _radius;
        private ReactiveVariable<float> _showTime;
        private ReactiveVariable<float> _currentTime;

        private ReactiveEvent<Vector3> _teleportEvent;

        private IDisposable _teleportEventDisposable;

        public void OnInit(Entity entity)
        {
            _zone = entity.AreaDamageZoneView;
            _radius = entity.AreaDamageRadius;
            _showTime = entity.AreaDamageZoneShowTime;
            _currentTime = entity.AreaDamageZoneCurrentTime;
            _teleportEvent = entity.TeleportEvent;

            float diameter = _radius.Value * 2;
            _zone.transform.localScale = new Vector3(diameter, _zone.transform.localScale.y, diameter);
            _zone.SetActive(false);

            _teleportEventDisposable = _teleportEvent.Subscribe(OnTeleport);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_zone.activeSelf == false)
                return;

            _currentTime.Value -= deltaTime;

            if (_currentTime.Value <= 0)
                _zone.SetActive(false);
        }

        public void OnDispose()
        {
            _teleportEventDisposable.Dispose();
        }

        private void OnTeleport(Vector3 position)
        {
            _currentTime.Value = _showTime.Value;
            _zone.SetActive(true);
        }
    }
}
