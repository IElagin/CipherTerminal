using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Attack
{
    public class AttackDelayEndTriggerSystem : IInitializableSystem, IDisposableSystem
    {
        private ReactiveEvent _attackDelayEndEvent;
        private ReactiveVariable<float> _delay;
        private ReactiveVariable<float> _attackProcessCurrentTime;

        private ReactiveEvent _startAttackEvent;

        private bool _alreadyAttacked;

        private IDisposable _timerDisposable;
        private IDisposable _startAttackDisposable;

        public void OnInit(Entity entity)
        {
            _attackDelayEndEvent = entity.AttackDelayEndEvent;
            _delay = entity.AttackDelayTime;
            _attackProcessCurrentTime = entity.AttackProcessCurrentTime;
            _startAttackEvent = entity.StartAttackEvent;

            _timerDisposable = _attackProcessCurrentTime.Subscribe(OnTimerChanged);
            _startAttackDisposable = _startAttackEvent.Subscribe(OnStartAttack);
        }

        private void OnStartAttack()
        {
            _alreadyAttacked = false;
        }

        private void OnTimerChanged(float arg1, float currentTime)
        {
            if (_alreadyAttacked)
                return;

            if (currentTime >= _delay.Value)
            {
                Debug.Log("Задержка перед атакой закончилась");
                _attackDelayEndEvent.Invoke();
                _alreadyAttacked = true;
            }
        }

        public void OnDispose()
        {
            _timerDisposable.Dispose();
            _startAttackDisposable.Dispose();
        }
    }
}
