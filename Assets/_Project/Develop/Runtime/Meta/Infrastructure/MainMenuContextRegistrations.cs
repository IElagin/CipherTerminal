using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.UI.MainMenu;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using VContainer;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public static class MainMenuContextRegistrations
    {
        public static void Process(IContainerBuilder builder, MainMenuScreenView view,
            TerminalKeyboard keyboard, UIRoot root)
        {
            builder.RegisterInstance(view);
            builder.RegisterInstance(keyboard);
            builder.RegisterInstance(root);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            builder.Register<MainMenuPopupService>(Lifetime.Scoped);
            builder.Register(CreateScreenPresenter, Lifetime.Scoped);
        }

        private static MainMenuScreenPresenter CreateScreenPresenter(IObjectResolver container)
            => new MainMenuScreenPresenter(container.Resolve<MainMenuScreenView>(),
                container.Resolve<TerminalKeyboard>(), container.Resolve<SceneNavigator>(),
                container.Resolve<IAudioService>(), container.Resolve<PlayerProgressService>(),
                container.Resolve<MainMenuPopupService>(), container.Resolve<ProjectPresentersFactory>());
    }
}
