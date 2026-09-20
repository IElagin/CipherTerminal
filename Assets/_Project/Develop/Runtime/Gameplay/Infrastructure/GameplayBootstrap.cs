using System.Threading;
using Assets._Project.Develop.Runtime.Gameplay.Configs;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayBootstrap : SceneBootstrap
    {
        [SerializeField] private GameplayScreenView _view;
        [SerializeField] private TerminalKeyboard _keyboard;

        private GameplayScreenPresenter _presenter;
        private GameplayProgressTracker _progressTracker;

        public override UniTask Initialize(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GameplayInputArgs gameplayInputArgs = container.Resolve<GameplayInputArgs>();
            SequenceConfig config = container.Resolve<ConfigsProviderService>().GetConfig<SequenceConfig>();
            container.Resolve<SequenceSession>().Initialize(config.GetSymbols(gameplayInputArgs.Mode), config.Length);
            _progressTracker = container.Resolve<GameplayProgressTracker>();
            _presenter = container.Resolve<GameplayScreenPresenter>();
            _presenter.Initialize();
            return UniTask.CompletedTask;
        }

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
            => GameplayContextRegistrations.Process(builder, (GameplayInputArgs)sceneArgs, _view, _keyboard);

        public override void Run()
        {
            _progressTracker.Run();
            _presenter.Run();
        }
    }
}
