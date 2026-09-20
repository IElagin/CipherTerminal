using VContainer;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.MainMenu;
using Assets._Project.Develop.Runtime.UI.ResetStatistics;
using Assets._Project.Develop.Runtime.Utilities.Audio;

namespace Assets._Project.Develop.Runtime.UI
{
    public sealed class ProjectPresentersFactory
    {
        private readonly IObjectResolver _container;

        public ProjectPresentersFactory(IObjectResolver container) => _container = container;

        public ProgressPanelPresenter CreateProgressPanelPresenter(ProgressPanelView view)
            => new ProgressPanelPresenter(view, _container.Resolve<PlayerProgressService>());

        public ResetStatisticsPopupPresenter CreateResetStatisticsPopupPresenter(ResetStatisticsPopupView view)
            => new ResetStatisticsPopupPresenter(view, _container.Resolve<PlayerProgressService>(),
                _container.Resolve<IAudioService>());
    }
}
