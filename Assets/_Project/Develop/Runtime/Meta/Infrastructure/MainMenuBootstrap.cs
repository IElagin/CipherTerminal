using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public class MainMenuBootstrap : SceneBootstrap
    {
        [SerializeField] private MainMenuController _controller;

        public override UniTask Initialize(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SceneNavigator navigator = container.Resolve<SceneNavigator>();
            IAudioService audio = container.Resolve<IAudioService>();
            PlayerProgressService progress = container.Resolve<PlayerProgressService>();
            MainMenuController controller = container.Resolve<MainMenuController>();

            controller.Initialize(navigator, audio, progress);

            return UniTask.CompletedTask;
        }

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
        {
            MainMenuContextRegistrations.Process(builder, _controller);
        }

        public override void Run()
        {
            _controller.Run();
        }
    }
}
