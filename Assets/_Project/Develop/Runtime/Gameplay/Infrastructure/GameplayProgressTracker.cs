using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public sealed class GameplayProgressTracker : IDisposable
    {
        private readonly GameplayLoop _loop;
        private readonly PlayerProgressService _progress;

        private bool _subscribed;
        private bool _recorded;

        public GameplayProgressTracker(GameplayLoop loop, PlayerProgressService progress)
        {
            _loop = loop ?? throw new ArgumentNullException(nameof(loop));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public void Run()
        {
            if (_subscribed)
                return;

            _subscribed = true;
            _loop.Finished += OnFinished;
        }

        public void Dispose()
        {
            if (_subscribed == false)
                return;

            _subscribed = false;
            _loop.Finished -= OnFinished;
        }

        private void OnFinished(SequenceState state)
        {
            if (_recorded)
                return;

            _recorded = true;
            RecordAndReportAsync(state).Forget(AsyncErrors.Report);
        }

        private async UniTask RecordAndReportAsync(SequenceState state)
        {
            ProgressOperationResult result = await _progress.RecordResultAsync(state);

            if (result.Status == ProgressOperationStatus.Unavailable ||
                result.Status == ProgressOperationStatus.Busy ||
                result.Status == ProgressOperationStatus.Failed)
            {
                Debug.LogError(_progress.Error ?? "Прогресс недоступен");
                return;
            }

            ProgressSnapshot snapshot = _progress.Snapshot;
            string sign = result.GoldDelta > 0 ? "+" : "";
            string outcome = state == SequenceState.Won ? "победа" : "поражение";
            Debug.Log("Экономика: " + outcome + ", " + sign + result.GoldDelta +
                      " золота; баланс " + snapshot.Gold);

            if (result.Status == ProgressOperationStatus.SavedInMemoryOnly)
                Debug.LogError(_progress.Error);
        }
    }
}
