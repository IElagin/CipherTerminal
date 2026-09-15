using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayBootstrap : SceneBootstrap
    {
        [SerializeField] private GameplayController _controller;

        private IObjectResolver _container;
        private GameplayProgressTracker _progressTracker;

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
        {
            if (sceneArgs is not GameplayInputArgs args)
                throw new ArgumentException("Gameplay requires GameplayInputArgs", nameof(sceneArgs));

            if (args.LevelNumber <= 0)
                throw new ArgumentException("Gameplay level number must be positive", nameof(sceneArgs));

            if (!Enum.IsDefined(typeof(SequenceMode), args.Mode))
                throw new ArgumentException("Gameplay mode is invalid", nameof(sceneArgs));

            GameplayContextRegistrations.Process(builder, args);
            builder.Register(_ => _controller, Lifetime.Scoped);
        }

        public override UniTask InitializeAsync(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _container = container;
            SceneNavigator navigator = _container.Resolve<SceneNavigator>();
            _progressTracker = _container.Resolve<GameplayProgressTracker>();
            GameplayController controller = _container.Resolve<GameplayController>();
            controller.Configure(_container.Resolve<GameplayLoop>(), navigator, _container.Resolve<IAudioService>());
            return UniTask.CompletedTask;
        }

        public override void Run()
        {
            _progressTracker.Run();
            _controller.Run();
        }
    }
}
