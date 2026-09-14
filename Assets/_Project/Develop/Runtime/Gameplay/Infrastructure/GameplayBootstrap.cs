using System.Collections;
using UnityEngine;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagement;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Utilities.Audio;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayBootstrap : SceneBootstrap
    {
        [SerializeField] private GameplayController _controller;

        private DIContainer _container;

        public override void ProcessRegistrations(DIContainer container, IInputSceneArgs sceneArgs = null)
        {
            _container = container;

            if (sceneArgs is not GameplayInputArgs args)
                throw new System.ArgumentException("GameplayInputArgs required", nameof(sceneArgs));

            GameplayContextRegistrations.Process(container, args);
            container.RegisterAsSingle(c => new SceneNavigator(c.Resolve<SceneSwitcherService>(), c.Resolve<ICoroutinesPerformer>()));
        }

        public override IEnumerator Initialize()
        {
            SceneNavigator navigator = _container.Resolve<SceneNavigator>();
            _controller.Configure(_container.Resolve<GameplayLoop>(), navigator, _container.Resolve<IAudioService>());
            yield break;
        }

        public override void Run()
        {
            _controller.Run();
        }
    }
}
