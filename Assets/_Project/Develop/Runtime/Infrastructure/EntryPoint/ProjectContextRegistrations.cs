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
            builder.Register(CreateConfigsProvider, Lifetime.Singleton);
            builder.Register<JsonSerializer>(Lifetime.Singleton).As<IDataSerializer>();
            builder.Register<MapDataKeysStorage>(Lifetime.Singleton).As<IDataKeysStorage>();
            builder.Register(CreateLocalFileRepository, Lifetime.Singleton).As<IDataRepository>();
            builder.Register<SaveLoadService>(Lifetime.Singleton).As<ISaveLoadService>();
            builder.Register<WalletService>(Lifetime.Singleton);
            builder.Register<StatisticsService>(Lifetime.Singleton);
            builder.Register(CreatePlayerDataProvider, Lifetime.Singleton);
            builder.Register(CreatePlayerProgress, Lifetime.Singleton);
            builder.Register<SceneLoaderService>(Lifetime.Singleton);
            builder.Register(CreateSceneSwitcher, Lifetime.Singleton);

            PlayerProgressService CreatePlayerProgress(IObjectResolver container)
            {
                PlayerDataProvider provider = container.Resolve<PlayerDataProvider>();
                WalletService wallet = container.Resolve<WalletService>();
                StatisticsService statistics = container.Resolve<StatisticsService>();
                EconomyRules rules = container.Resolve<ConfigsProviderService>().GetConfig<EconomyConfig>().Rules;

                return new PlayerProgressService(provider, wallet, statistics, rules, projectToken);
            }

            SceneSwitcherService CreateSceneSwitcher(IObjectResolver container)
            {
                SceneLoaderService loader = container.Resolve<SceneLoaderService>();
                ILoadingScreen screen = container.Resolve<ILoadingScreen>();

                return new SceneSwitcherService(loader, screen, container, projectToken);
            }
        }

        private static ConfigsProviderService CreateConfigsProvider(IObjectResolver container)
        {
            return new ConfigsProviderService(container.Resolve<IConfigsLoader>());
        }

        private static LocalFileDataRepository CreateLocalFileRepository(IObjectResolver container)
        {
            return new LocalFileDataRepository(Path.Combine(Application.persistentDataPath, "Saves"), "json");
        }

        private static PlayerDataProvider CreatePlayerDataProvider(IObjectResolver container)
        {
            ISaveLoadService saveLoad = container.Resolve<ISaveLoadService>();
            EconomyRules rules = container.Resolve<ConfigsProviderService>().GetConfig<EconomyConfig>().Rules;

            return new PlayerDataProvider(saveLoad, rules);
        }
    }
}
