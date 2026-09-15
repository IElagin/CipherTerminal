using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public class MainMenuBootstrap : SceneBootstrap
    {
        [SerializeField] private MainMenuController _controller;

        private IObjectResolver _container;

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
        {
            MainMenuContextRegistrations.Process(builder);
            builder.Register(_ => _controller, Lifetime.Scoped);
        }

        public override UniTask InitializeAsync(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _container = container;
            SceneNavigator navigator = _container.Resolve<SceneNavigator>();
            MainMenuController controller = _container.Resolve<MainMenuController>();
            controller.Configure(navigator, _container.Resolve<IAudioService>());
            return UniTask.CompletedTask;
        }

        public override void Run()
        {
            _controller.Run();
        }
    }
}
