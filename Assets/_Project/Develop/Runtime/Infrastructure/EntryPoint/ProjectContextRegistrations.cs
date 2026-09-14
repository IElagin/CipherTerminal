using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagement;
using Assets._Project.Develop.Runtime.Utilities.LoadingScreen;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Object = UnityEngine.Object;

namespace Assets._Project.Develop.Runtime.Infrastructure.EntryPoint
{
    public class ProjectContextRegistrations
    {
        public static void Process(DIContainer container)
        {
            container.RegisterAsSingle<ICoroutinesPerformer>(CreateCoroutinesPerformer);
            container.RegisterAsSingle(CreateConfigsProviderService);
            container.RegisterAsSingle(CreateResourcesAssetLoader);
            container.RegisterAsSingle<IAudioService>(CreateAudioService);
            container.RegisterAsSingle(CreateSceneLoaderService);
            container.RegisterAsSingle<ILoadingScreen>(CreateLoadingScreen);
            container.RegisterAsSingle(CreateSceneSwitcherService);
        }

        private static SceneSwitcherService CreateSceneSwitcherService(DIContainer c)
            => new SceneSwitcherService(c.Resolve<SceneLoaderService>(), c.Resolve<ILoadingScreen>(), c);

        private static StandardLoadingScreen CreateLoadingScreen(DIContainer c)
        {
            ResourcesAssetLoader resources = c.Resolve<ResourcesAssetLoader>();
            StandardLoadingScreen prefab = resources.Load<StandardLoadingScreen>("Utilities/StandardLoadingScreen");
            return Object.Instantiate(prefab);
        }

        private static SceneLoaderService CreateSceneLoaderService(DIContainer c)
            => new SceneLoaderService();

        private static AudioService CreateAudioService(DIContainer c)
        {
            ResourcesAssetLoader resources = c.Resolve<ResourcesAssetLoader>();
            AudioService prefab = resources.Load<AudioService>("Utilities/AudioService");
            return Object.Instantiate(prefab);
        }

        private static ConfigsProviderService CreateConfigsProviderService(DIContainer c)
        {
            ResourcesAssetLoader resources = c.Resolve<ResourcesAssetLoader>();
            var resourcesConfigsLoader = new ResourcesConfigsLoader(resources);
            return new ConfigsProviderService(resourcesConfigsLoader);
        }

        private static ResourcesAssetLoader CreateResourcesAssetLoader(DIContainer c)
            => new ResourcesAssetLoader();

        private static CoroutinesPerformer CreateCoroutinesPerformer(DIContainer c)
        {
            ResourcesAssetLoader resources = c.Resolve<ResourcesAssetLoader>();
            CoroutinesPerformer prefab = resources.Load<CoroutinesPerformer>("Utilities/CoroutinesPerformer");
            return Object.Instantiate(prefab);
        }
    }
}
