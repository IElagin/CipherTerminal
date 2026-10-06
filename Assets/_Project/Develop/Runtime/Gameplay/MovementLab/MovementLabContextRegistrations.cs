using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.UI.MovementLab;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Gameplay.MovementLab
{
    public static class MovementLabContextRegistrations
    {
        public static void Process(IContainerBuilder builder)
        {
            EntitiesLifeContext entitiesLifeContext = null;
            MonoEntitiesFactory monoEntitiesFactory = null;

            builder.Register(CreateEntitiesFactory, Lifetime.Scoped);
            builder.Register(container => entitiesLifeContext ??= CreateEntitiesLifeContext(container), Lifetime.Transient);
            builder.Register(CreateCollidersRegistryService, Lifetime.Scoped);
            builder.Register(container => monoEntitiesFactory ??= CreateMonoEntitiesFactory(container), Lifetime.Transient);
            builder.RegisterBuildCallback(container => container.Resolve<MonoEntitiesFactory>().Initialize());

            // VContainer disposes tracked services in reverse creation order; the course releases entities before views.
            builder.RegisterDisposeCallback(container =>
            {
                entitiesLifeContext.Dispose();
                monoEntitiesFactory.Dispose();
            });

            builder.Register(CreateSession, Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            builder.Register(CreateUIRoot, Lifetime.Scoped);
            builder.Register(CreateScreenPresenter, Lifetime.Scoped);
        }

        private static EntitiesLifeContext CreateEntitiesLifeContext(IObjectResolver container)
            => new EntitiesLifeContext();

        private static CollidersRegistryService CreateCollidersRegistryService(IObjectResolver container)
            => new CollidersRegistryService();

        private static MonoEntitiesFactory CreateMonoEntitiesFactory(IObjectResolver container)
        {
            return new MonoEntitiesFactory(container.Resolve<ResourcesAssetLoader>(),
                container.Resolve<EntitiesLifeContext>(), container.Resolve<CollidersRegistryService>());
        }

        private static EntitiesFactory CreateEntitiesFactory(IObjectResolver container)
        {
            return new EntitiesFactory(container.Resolve<EntitiesLifeContext>(),
                container.Resolve<MonoEntitiesFactory>(), container.Resolve<CollidersRegistryService>(),
                container.Resolve<ConfigsProviderService>());
        }

        private static MovementLabSession CreateSession(IObjectResolver container)
        {
            return new MovementLabSession(container.Resolve<EntitiesLifeContext>(),
                container.Resolve<EntitiesFactory>(), new MovementLabInput());
        }

        private static UIRoot CreateUIRoot(IObjectResolver container)
        {
            UIRoot prefab = container.Resolve<ResourcesAssetLoader>().Load<UIRoot>("UI/UIRoot");

            return Object.Instantiate(prefab);
        }

        private static MovementLabScreenPresenter CreateScreenPresenter(IObjectResolver container)
        {
            UIRoot root = container.Resolve<UIRoot>();
            MovementLabScreenView view = container.Resolve<ViewsFactory>()
                .Create<MovementLabScreenView>(ViewIDs.MovementLabScreen, root.HudLayer);

            return new MovementLabScreenPresenter(view, container.Resolve<SceneNavigator>());
        }
    }
}
