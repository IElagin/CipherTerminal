using System.IO;
using System.Threading;
using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Meta.Configs;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers;
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
            builder.Register<JsonSerializer>(Lifetime.Singleton).As<IDataSerializer>();
            builder.Register<MapDataKeysStorage>(Lifetime.Singleton).As<IDataKeysStorage>();
            builder.Register(c => new LocalFileDataRepository(
                Path.Combine(Application.persistentDataPath, "Saves"), "json"), Lifetime.Singleton)
                .As<IDataRepository>();
            builder.Register(c => new SaveLoadService(c.Resolve<IDataSerializer>(),
                c.Resolve<IDataKeysStorage>(), c.Resolve<IDataRepository>()), Lifetime.Singleton)
                .As<ISaveLoadService>();
            builder.Register<WalletService>(Lifetime.Singleton);
            builder.Register<StatisticsService>(Lifetime.Singleton);
            builder.Register(c => new PlayerDataProvider(c.Resolve<ISaveLoadService>(),
                c.Resolve<ConfigsProviderService>().GetConfig<EconomyConfig>().Rules), Lifetime.Singleton);
            builder.Register(c => new PlayerProgressService(
                c.Resolve<PlayerDataProvider>(),
                c.Resolve<WalletService>(),
                c.Resolve<StatisticsService>(),
                c.Resolve<ConfigsProviderService>().GetConfig<EconomyConfig>().Rules,
                projectToken), Lifetime.Singleton);
            builder.Register<SceneLoaderService>(Lifetime.Singleton);
            builder.Register(c => new SceneSwitcherService(c.Resolve<SceneLoaderService>(),
                c.Resolve<ILoadingScreen>(), c, projectToken), Lifetime.Singleton);
        }
    }
}
