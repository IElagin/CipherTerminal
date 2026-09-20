using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.UI.MainMenu;
using Assets._Project.Develop.Runtime.UI;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public static class MainMenuContextRegistrations
    {
        private const string UIRootPath = "UI/UIRoot";

        public static void Process(IContainerBuilder builder, TerminalKeyboard keyboard)
        {
            builder.RegisterInstance(keyboard);
            builder.Register(CreateUIRoot, Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            builder.Register<MainMenuPopupService>(Lifetime.Scoped);
            builder.Register(CreateScreenPresenter, Lifetime.Scoped);
        }

        private static UIRoot CreateUIRoot(IObjectResolver container)
        {
            UIRoot prefab = container.Resolve<ResourcesAssetLoader>().Load<UIRoot>(UIRootPath);

            return Object.Instantiate(prefab);
        }

        private static MainMenuScreenPresenter CreateScreenPresenter(IObjectResolver container)
        {
            UIRoot root = container.Resolve<UIRoot>();
            MainMenuScreenView view = container.Resolve<ViewsFactory>()
                .Create<MainMenuScreenView>(ViewIDs.MainMenuScreen, root.HudLayer);

            return new MainMenuScreenPresenter(view,
                container.Resolve<TerminalKeyboard>(), container.Resolve<SceneNavigator>(),
                container.Resolve<IAudioService>(), container.Resolve<PlayerProgressService>(),
                container.Resolve<MainMenuPopupService>(), container.Resolve<ProjectPresentersFactory>());
        }
    }
}
