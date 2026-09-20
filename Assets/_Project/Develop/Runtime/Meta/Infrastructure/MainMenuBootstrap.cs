using System.Threading;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.UI.MainMenu;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public class MainMenuBootstrap : SceneBootstrap
    {
        [SerializeField] private MainMenuScreenView _view;
        [SerializeField] private TerminalKeyboard _keyboard;
        [SerializeField] private UIRoot _uiRoot;

        private MainMenuScreenPresenter _presenter;

        public override UniTask Initialize(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _presenter = container.Resolve<MainMenuScreenPresenter>();
            _presenter.Initialize();
            return UniTask.CompletedTask;
        }

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
            => MainMenuContextRegistrations.Process(builder, _view, _keyboard, _uiRoot);

        public override void Run() => _presenter.Run();
    }
}
