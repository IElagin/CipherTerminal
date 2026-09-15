using System.Threading;
using VContainer;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.LoadingScreen;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Infrastructure.EntryPoint
{
    public static class ProjectContextRegistrations
    {
        public static void Process(IContainerBuilder builder, ILoadingScreen loadingScreen,
            AudioService audioService, CancellationToken projectToken)
        {
            builder.RegisterInstance(loadingScreen);
            builder.RegisterInstance(audioService).As<IAudioService>();
            builder.Register<ResourcesAssetLoader>(Lifetime.Singleton);
            builder.Register<ResourcesConfigsLoader>(Lifetime.Singleton).As<IConfigsLoader>();
            builder.Register(c => new ConfigsProviderService(c.Resolve<IConfigsLoader>()), Lifetime.Singleton);
            builder.Register<SceneLoaderService>(Lifetime.Singleton);
            builder.Register(c => new SceneSwitcherService(c.Resolve<SceneLoaderService>(),
                c.Resolve<ILoadingScreen>(), c, projectToken), Lifetime.Singleton);
        }
    }
}
