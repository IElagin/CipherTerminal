using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using VContainer;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public static class GameplayContextRegistrations
    {
        public static void Process(IContainerBuilder builder, GameplayInputArgs gameplayInputArgs,
            GameplayScreenView view, TerminalKeyboard keyboard)
        {
            builder.RegisterInstance(gameplayInputArgs);
            builder.RegisterInstance(view);
            builder.RegisterInstance(keyboard);
            builder.Register(CreateSequenceGenerator, Lifetime.Scoped);
            builder.Register<SequenceSession>(Lifetime.Scoped);
            builder.Register<GameplayLoop>(Lifetime.Scoped);
            builder.Register<GameplayProgressTracker>(Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            builder.Register(CreateScreenPresenter, Lifetime.Scoped);
        }

        private static SequenceGenerator CreateSequenceGenerator(IObjectResolver container)
            => new SequenceGenerator(new System.Random());

        private static GameplayScreenPresenter CreateScreenPresenter(IObjectResolver container)
            => new GameplayScreenPresenter(container.Resolve<GameplayScreenView>(),
                container.Resolve<TerminalKeyboard>(), container.Resolve<GameplayLoop>(),
                container.Resolve<SceneNavigator>(), container.Resolve<IAudioService>(),
                container.Resolve<PlayerProgressService>());
    }
}
