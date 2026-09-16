using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Gameplay.Configs;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayBootstrap : SceneBootstrap
    {
        [SerializeField] private GameplayController _controller;

        private GameplayProgressTracker _progressTracker;

        public override UniTask Initialize(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GameplayInputArgs gameplayInputArgs = container.Resolve<GameplayInputArgs>();
            ConfigsProviderService configs = container.Resolve<ConfigsProviderService>();
            SequenceSession session = container.Resolve<SequenceSession>();
            GameplayLoop loop = container.Resolve<GameplayLoop>();
            SceneNavigator navigator = container.Resolve<SceneNavigator>();
            IAudioService audio = container.Resolve<IAudioService>();
            GameplayController controller = container.Resolve<GameplayController>();
            _progressTracker = container.Resolve<GameplayProgressTracker>();

            SequenceConfig config = configs.GetConfig<SequenceConfig>();
            session.Initialize(config.GetSymbols(gameplayInputArgs.Mode), config.Length);
            controller.Initialize(loop, navigator, audio);

            return UniTask.CompletedTask;
        }

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
        {
            if (sceneArgs is not GameplayInputArgs gameplayInputArgs)
                throw new ArgumentException("Gameplay requires GameplayInputArgs", nameof(sceneArgs));

            gameplayInputArgs.Validate(nameof(sceneArgs));

            GameplayContextRegistrations.Process(builder, gameplayInputArgs, _controller);
        }

        public override void Run()
        {
            _progressTracker.Run();
            _controller.Run();
        }
    }
}
