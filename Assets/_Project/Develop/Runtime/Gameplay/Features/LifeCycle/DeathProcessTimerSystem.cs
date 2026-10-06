using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.LifeCycle
{
    public class DeathProcessTimerSystem : IInitializableSystem, IDisposableSystem, IUpdatableSystem
    {
        private ReactiveVariable<bool> _isDead;
        private ReactiveVariable<bool> _inDeathProcess;
        private ReactiveVariable<float> _initialTime;
        private ReactiveVariable<float> _currentTime;

        private IDisposable _isDeadChangedDisposable;

        public void OnInit(Entity entity)
        {
            _isDead = entity.IsDead;
            _inDeathProcess = entity.InDeathProcess;
            _initialTime = entity.DeathProcessInitialTime;
            _currentTime = entity.DeathProcessCurrentTime;

            _isDeadChangedDisposable = _isDead.Subscribe(OnIsDeadChanged);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_inDeathProcess.Value == false)
                return;

            _currentTime.Value -= deltaTime;

            if (TimeIsOver())
                _inDeathProcess.Value = false;
        }

        public void OnDispose()
        {
            _isDeadChangedDisposable.Dispose();
        }

        private void OnIsDeadChanged(bool arg1, bool isDead)
        {
            if (isDead)
            {
                _currentTime.Value = _initialTime.Value;
                _inDeathProcess.Value = true;
            }
        }

        private bool TimeIsOver() => _currentTime.Value <= 0;
    }
}
