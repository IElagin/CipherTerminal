using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Energy
{
    public class EnergyRegenerationSystem : IInitializableSystem, IUpdatableSystem
    {
        private ReactiveVariable<float> _maxEnergy;
        private ReactiveVariable<float> _currentEnergy;
        private ReactiveVariable<float> _regenerationPercent;
        private ReactiveVariable<float> _initialTime;
        private ReactiveVariable<float> _currentTime;

        public void OnInit(Entity entity)
        {
            _maxEnergy = entity.MaxEnergy;
            _currentEnergy = entity.CurrentEnergy;
            _regenerationPercent = entity.EnergyRegenerationPercent;
            _initialTime = entity.EnergyRegenerationInitialTime;
            _currentTime = entity.EnergyRegenerationCurrentTime;

            _currentTime.Value = _initialTime.Value;
        }

        public void OnUpdate(float deltaTime)
        {
            _currentTime.Value -= deltaTime;

            if (_currentTime.Value > 0)
                return;

            _currentTime.Value = _initialTime.Value;

            if (_currentEnergy.Value >= _maxEnergy.Value)
                return;

            _currentEnergy.Value = MathF.Min(_currentEnergy.Value + _maxEnergy.Value * _regenerationPercent.Value, _maxEnergy.Value);
            Debug.Log("Энергия восстановлена: " + _currentEnergy.Value);
        }
    }
}
