using Cysharp.Threading.Tasks;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.UI.ResetStatistics
{
    public sealed class ResetStatisticsPopupPresenter : PopupPresenterBase
    {
        private readonly ResetStatisticsPopupView _view;
        private readonly PlayerProgressService _progress;
        private readonly IAudioService _audio;
        private bool _submitting;
        private bool _applied;
        private bool _closing;
        private bool _disposed;

        public ResetStatisticsPopupPresenter(ResetStatisticsPopupView view,
            PlayerProgressService progress, IAudioService audio)
        {
            _view = view;
            _progress = progress;
            _audio = audio;
        }

        protected override PopupViewBase PopupView => _view;

        public override void Initialize()
        {
            _progress.Changed += OnProgressChanged;
            Render();
        }

        public void RequestCancel()
        {
            if (_submitting || _closing || _disposed)
                return;

            OnCloseRequest();
        }

        public override void Dispose()
        {
            _disposed = true;
            base.Dispose();
            _view.ConfirmClicked -= OnConfirmClicked;
            _progress.Changed -= OnProgressChanged;
        }

        protected override void OnPreShow()
        {
            base.OnPreShow();
            _view.ConfirmClicked += OnConfirmClicked;
        }

        protected override void OnPreHide()
        {
            _closing = true;
            base.OnPreHide();
            _view.ConfirmClicked -= OnConfirmClicked;
            _view.SetInteraction(false, false);
        }

        private void OnConfirmClicked() => ConfirmAsync().Forget(AsyncErrors.Report);

        private async UniTask ConfirmAsync()
        {
            if (_submitting || _applied || _closing || _disposed)
                return;

            _submitting = true;
            _view.SetInteraction(false, false);
            ProgressOperationResult result = await _progress.ResetStatisticsAsync();

            if (_disposed)
                return;

            _submitting = false;
            _applied = result.Status == ProgressOperationStatus.Completed ||
                       result.Status == ProgressOperationStatus.SavedInMemoryOnly;
            _audio.Play(result.Status == ProgressOperationStatus.Completed ? AudioCue.Key : AudioCue.Error);

            if (result.Status == ProgressOperationStatus.Completed)
            {
                OnCloseRequest();
                return;
            }

            Render();
        }

        private void OnProgressChanged(ProgressSnapshot snapshot) => Render();

        private void Render()
        {
            int gold = _progress.IsReady ? _progress.Snapshot.Gold : 0;
            int cost = _progress.StatisticsResetCost;
            _view.Render(cost, gold);
            string message = _progress.Error;

            if (_applied)
                message = "Статистика сброшена. " + _progress.Error;
            else if (_progress.IsReady && gold < cost)
            {
                int missingGold = cost - gold;
                message = "Недостаточно золота. Не хватает " + missingGold + ".";
            }

            _view.ShowStatus(message ?? string.Empty);
            _view.SetInteraction(_progress.IsReady && gold >= cost && _applied == false &&
                                 _submitting == false && _closing == false,
                _submitting == false && _closing == false);
        }
    }
}
