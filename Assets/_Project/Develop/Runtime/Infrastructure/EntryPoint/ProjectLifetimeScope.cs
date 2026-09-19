using System.Threading;
using VContainer;
using VContainer.Unity;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Infrastructure.EntryPoint
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        private CancellationTokenSource _projectLifetime;
        private SceneSwitcherService _sceneSwitcher;

        public void Initialize()
        {
            _projectLifetime = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
            Build();
            _sceneSwitcher = Container.Resolve<SceneSwitcherService>();
        }

        public CancellationToken Token => _projectLifetime.Token;

        protected override void Configure(IContainerBuilder builder)
        {
            ProjectContextRegistrations.Process(builder, transform, Token);
        }

        protected override void OnDestroy()
        {
            _projectLifetime?.Cancel();
            _sceneSwitcher?.Dispose();
            base.OnDestroy();
            _projectLifetime?.Dispose();
        }
    }
}
