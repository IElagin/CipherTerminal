using System.Threading;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.UI.MovementLab;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Gameplay.MovementLab
{
    public sealed class MovementLabBootstrap : SceneBootstrap
    {
        private MovementLabSession _session;
        private MovementLabScreenPresenter _presenter;
        private SceneNavigator _navigator;
        private bool _isRunning;

        public override UniTask Initialize(IObjectResolver container, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _session = container.Resolve<MovementLabSession>();
            _presenter = container.Resolve<MovementLabScreenPresenter>();
            _navigator = container.Resolve<SceneNavigator>();
            _session.Initialize();
            _presenter.Initialize();

            return UniTask.CompletedTask;
        }

        public override void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null)
            => MovementLabContextRegistrations.Process(builder);

        public override void Run()
        {
            _session.Run();
            _isRunning = true;
        }

        private void Update()
        {
            if (_isRunning == false || _navigator.IsLeaving)
                return;

            _presenter.Update();

            if (_navigator.IsLeaving == false)
                _session.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_isRunning == false || _navigator.IsLeaving)
                return;

            _session.FixedUpdate(Time.fixedDeltaTime);
        }
    }
}
