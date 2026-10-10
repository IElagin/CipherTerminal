using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using System;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Attack
{
    public class StartAttackSystem : IInitializableSystem, IDisposableSystem
    {
        private ReactiveEvent _startAttackRequest;
        private ReactiveEvent _startAttackEvent;
        private ReactiveVariable<bool> _inAttackProcess;
        private ICompositeCondition _canStartAttack;

        private IDisposable _attackRequestDisposable;

        public void OnInit(Entity entity)
        {
            _startAttackRequest = entity.StartAttackRequest;
            _startAttackEvent = entity.StartAttackEvent;
            _inAttackProcess = entity.InAttackProcess;
            _canStartAttack = entity.CanStartAttack;

            _attackRequestDisposable = _startAttackRequest.Subscribe(OnAttackRequest);
        }

        private void OnAttackRequest()
        {
            if (_canStartAttack.Evaluate())
            {
                _inAttackProcess.Value = true;
                _startAttackEvent.Invoke();
                Debug.Log("Старт атаки");
            }
            else
            {
                Debug.Log("Не могу атаковать");
            }
        }

        public void OnDispose()
        {
            _attackRequestDisposable.Dispose();
        }
    }
}
