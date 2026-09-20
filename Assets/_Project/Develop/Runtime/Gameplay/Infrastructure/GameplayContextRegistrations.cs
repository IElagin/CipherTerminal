using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using UnityEngine;
using VContainer;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public static class GameplayContextRegistrations
    {
        private const string UIRootPath = "UI/UIRoot";

        public static void Process(IContainerBuilder builder, GameplayInputArgs gameplayInputArgs,
            TerminalKeyboard keyboard)
        {
            builder.RegisterInstance(gameplayInputArgs);
            builder.RegisterInstance(keyboard);
            builder.Register(CreateUIRoot, Lifetime.Scoped);
            builder.Register(CreateSequenceGenerator, Lifetime.Scoped);
            builder.Register<SequenceSession>(Lifetime.Scoped);
            builder.Register<GameplayLoop>(Lifetime.Scoped);
            builder.Register<GameplayProgressTracker>(Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            builder.Register(CreateScreenPresenter, Lifetime.Scoped);
        }

        private static SequenceGenerator CreateSequenceGenerator(IObjectResolver container)
            => new SequenceGenerator(new System.Random());

        private static UIRoot CreateUIRoot(IObjectResolver container)
        {
            UIRoot prefab = container.Resolve<ResourcesAssetLoader>().Load<UIRoot>(UIRootPath);

            return Object.Instantiate(prefab);
        }

        private static GameplayScreenPresenter CreateScreenPresenter(IObjectResolver container)
        {
            UIRoot root = container.Resolve<UIRoot>();
            GameplayScreenView view = container.Resolve<ViewsFactory>()
                .Create<GameplayScreenView>(ViewIDs.GameplayScreen, root.HudLayer);

            return new GameplayScreenPresenter(view,
                container.Resolve<TerminalKeyboard>(), container.Resolve<GameplayLoop>(),
                container.Resolve<SceneNavigator>(), container.Resolve<IAudioService>(),
                container.Resolve<PlayerProgressService>());
        }
    }
}
