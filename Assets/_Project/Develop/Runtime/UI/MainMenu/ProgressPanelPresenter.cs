using System;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Core;

namespace Assets._Project.Develop.Runtime.UI.MainMenu
{
    public sealed class ProgressPanelPresenter : IPresenter
    {
        private readonly ProgressPanelView _view;
        private readonly PlayerProgressService _progress;
        private bool _interactionEnabled;

        public event Action ResetRequested;

        public ProgressPanelPresenter(ProgressPanelView view, PlayerProgressService progress)
        {
            _view = view;
            _progress = progress;
        }

        public void Initialize()
        {
            _view.ResetClicked += OnResetClicked;
            _progress.Changed += OnProgressChanged;
            Render();
        }

        public void SetInteractable(bool enabled)
        {
            _interactionEnabled = enabled;
            _view.SetInteractable(enabled && _progress.IsReady);
        }

        public void Dispose()
        {
            _view.ResetClicked -= OnResetClicked;
            _progress.Changed -= OnProgressChanged;
        }

        private void OnResetClicked() => ResetRequested?.Invoke();

        private void OnProgressChanged(ProgressSnapshot snapshot) => Render();

        private void Render()
        {
            if (_progress.IsReady)
            {
                ProgressSnapshot snapshot = _progress.Snapshot;
                _view.Render(snapshot.Gold, snapshot.Wins, snapshot.Losses, _progress.StatisticsResetCost);
            }
            else
                _view.ShowUnavailable();

            bool error = string.IsNullOrEmpty(_progress.Error) == false;
            _view.ShowStatus(error ? _progress.Error : "> Система готова", error);
            _view.SetInteractable(_interactionEnabled && _progress.IsReady);
        }
    }
}
