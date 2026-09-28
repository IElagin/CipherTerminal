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
            builder.Register(CreateSession, Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            builder.Register(CreateUIRoot, Lifetime.Scoped);
            builder.Register(CreateScreenPresenter, Lifetime.Scoped);
        }

        private static MovementLabSession CreateSession(IObjectResolver container)
        {
            var lifeContext = new EntitiesLifeContext();
            var monoEntitiesFactory = new MonoEntitiesFactory(container.Resolve<ResourcesAssetLoader>(), lifeContext);
            var entitiesFactory = new EntitiesFactory(lifeContext, monoEntitiesFactory,
                container.Resolve<ConfigsProviderService>());

            return new MovementLabSession(lifeContext, monoEntitiesFactory, entitiesFactory, new MovementLabInput());
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
