using System.Collections;
using UnityEngine;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagement;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Utilities.Audio;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public class MainMenuBootstrap : SceneBootstrap
    {
        [SerializeField] private MainMenuController _controller;

        private DIContainer _container;

        public override void ProcessRegistrations(DIContainer container, IInputSceneArgs sceneArgs = null)
        {
            _container = container;
            MainMenuContextRegistrations.Process(container);
            container.RegisterAsSingle(c => new SceneNavigator(c.Resolve<SceneSwitcherService>(), c.Resolve<ICoroutinesPerformer>()));
        }

        public override IEnumerator Initialize()
        {
            SceneNavigator navigator = _container.Resolve<SceneNavigator>();
            _controller.Configure(navigator, _container.Resolve<IAudioService>());
            yield break;
        }

        public override void Run()
        {
            _controller.Run();
        }
    }
}
