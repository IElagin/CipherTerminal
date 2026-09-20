using System;
using System.Threading;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.UI.Core
{
    public abstract class PopupPresenterBase : IPresenter
    {
        private CancellationTokenSource _processLifetime;

        public event Action<PopupPresenterBase> CloseRequest;

        protected abstract PopupViewBase PopupView { get; }

        public virtual void Initialize() { }

        public void Show()
        {
            KillProcess();
            _processLifetime = new CancellationTokenSource();
            ProcessShowAsync(_processLifetime.Token).Forget(AsyncErrors.Report);
        }

        public void Hide(Action onHidden = null)
        {
            KillProcess();
            _processLifetime = new CancellationTokenSource();
            ProcessHideAsync(onHidden, _processLifetime.Token).Forget(AsyncErrors.Report);
        }

        public virtual void Dispose()
        {
            KillProcess();
            PopupView.CloseRequest -= OnCloseRequest;
        }

        protected virtual void OnPreShow() => PopupView.CloseRequest += OnCloseRequest;

        protected virtual void OnPostShow() { }

        protected virtual void OnPreHide() => PopupView.CloseRequest -= OnCloseRequest;

        protected virtual void OnPostHide() { }

        protected void OnCloseRequest() => CloseRequest?.Invoke(this);

        private async UniTask ProcessShowAsync(CancellationToken cancellationToken)
        {
            OnPreShow();
            await PopupView.Show().ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            OnPostShow();
        }

        private async UniTask ProcessHideAsync(Action onHidden, CancellationToken cancellationToken)
        {
            OnPreHide();
            await PopupView.Hide().ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            OnPostHide();
            onHidden?.Invoke();
        }

        private void KillProcess()
        {
            CancellationTokenSource lifetime = _processLifetime;
            _processLifetime = null;
            lifetime?.Cancel();
            lifetime?.Dispose();
        }
    }
}
