using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Attack
{
    public class EndAttackSystem : IInitializableSystem, IDisposableSystem
    {
        private ReactiveEvent _endAttackEvent;
        private ReactiveVariable<bool> _inAttackProcess;
        private ReactiveVariable<float> _attackProcessInitialTime;
        private ReactiveVariable<float> _attackProcessCurrentTime;

        private IDisposable _timerDisposable;

        public void OnInit(Entity entity)
        {
            _endAttackEvent = entity.EndAttackEvent;
            _inAttackProcess = entity.InAttackProcess;
            _attackProcessInitialTime = entity.AttackProcessInitialTime;
            _attackProcessCurrentTime = entity.AttackProcessCurrentTime;

            _timerDisposable = _attackProcessCurrentTime.Subscribe(OnTimerChanged);
        }

        private void OnTimerChanged(float arg1, float currentTime)
        {
            if (TimeIsDone(currentTime))
            {
                Debug.Log("Конец атаки");
                _inAttackProcess.Value = false;
                _endAttackEvent.Invoke();
            }
        }

        public void OnDispose()
        {
            _timerDisposable.Dispose();
        }

        private bool TimeIsDone(float currentTime) => currentTime >= _attackProcessInitialTime.Value;
    }
}
