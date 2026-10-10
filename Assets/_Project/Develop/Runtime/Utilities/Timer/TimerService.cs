using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.Timer
{
    public class TimerService : IDisposable
    {
        private float _cooldown;

        private ReactiveEvent _cooldownEnded;

        private ReactiveVariable<float> _currentTime;

        private CancellationTokenSource _cooldownProcess;

        public TimerService(float cooldown)
        {
            _cooldown = cooldown;

            _cooldownEnded = new ReactiveEvent();
            _currentTime = new ReactiveVariable<float>();
        }

        public IReadOnlyEvent CooldownEnded => _cooldownEnded;
        public IReadOnlyVariable<float> CurrentTime => _currentTime;
        public bool IsOver => _currentTime.Value <= 0;

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            if (_cooldownProcess == null)
                return;

            _cooldownProcess.Cancel();
            _cooldownProcess.Dispose();
            _cooldownProcess = null;
        }

        public void Restart()
        {
            Stop();

            _cooldownProcess = new CancellationTokenSource();
            CooldownProcess(_cooldownProcess.Token).Forget(AsyncErrors.Report);
        }

        private async UniTask CooldownProcess(CancellationToken cancellationToken)
        {
            _currentTime.Value = _cooldown;

            while (IsOver == false)
            {
                _currentTime.Value -= Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }

            _cooldownEnded.Invoke();
        }
    }
}
