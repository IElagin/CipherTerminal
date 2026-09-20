using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.UI.ResetStatistics;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Core
{
    public abstract class PopupService : IDisposable
    {
        protected readonly ViewsFactory ViewsFactory;

        private readonly ProjectPresentersFactory _presentersFactory;
        private readonly Dictionary<PopupPresenterBase, PopupInfo> _presenterToInfo = new();

        protected PopupService(ViewsFactory viewsFactory, ProjectPresentersFactory presentersFactory)
        {
            ViewsFactory = viewsFactory;
            _presentersFactory = presentersFactory;
        }

        protected abstract Transform PopupLayer { get; }

        public ResetStatisticsPopupPresenter OpenResetStatisticsPopup(Action closedCallback)
        {
            ResetStatisticsPopupView view = ViewsFactory.Create<ResetStatisticsPopupView>(ViewIDs.ResetStatisticsPopup, PopupLayer);
            ResetStatisticsPopupPresenter popup = _presentersFactory.CreateResetStatisticsPopupPresenter(view);
            OnPopupCreated(popup, view, closedCallback);
            return popup;
        }

        public void ClosePopup(PopupPresenterBase popup)
        {
            popup.CloseRequest -= ClosePopup;
            popup.Hide(() => OnPopupHidden(popup));
        }

        public void Dispose()
        {
            foreach (PopupPresenterBase popup in _presenterToInfo.Keys)
            {
                popup.CloseRequest -= ClosePopup;
                DisposeFor(popup);
            }

            _presenterToInfo.Clear();
        }

        protected void OnPopupCreated(PopupPresenterBase popup, PopupViewBase view, Action closedCallback = null)
        {
            _presenterToInfo.Add(popup, new PopupInfo(view, closedCallback));
            popup.Initialize();
            popup.CloseRequest += ClosePopup;
            popup.Show();
        }

        private void OnPopupHidden(PopupPresenterBase popup)
        {
            Action closedCallback = _presenterToInfo[popup].ClosedCallback;
            DisposeFor(popup);
            _presenterToInfo.Remove(popup);

            // External code can unload the scene and dispose this service synchronously.
            closedCallback?.Invoke();
        }

        private void DisposeFor(PopupPresenterBase popup)
        {
            popup.Dispose();
            ViewsFactory.Release(_presenterToInfo[popup].View);
        }

        private class PopupInfo
        {
            public PopupInfo(PopupViewBase view, Action closedCallback)
            {
                View = view;
                ClosedCallback = closedCallback;
            }

            public PopupViewBase View { get; }
            public Action ClosedCallback { get; }
        }
    }
}
