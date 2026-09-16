using VContainer;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Gameplay.Presentation;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayContextRegistrations
    {
        public static void Process(IContainerBuilder builder, GameplayInputArgs gameplayInputArgs,
            GameplayController controller)
        {
            builder.RegisterInstance(gameplayInputArgs);
            builder.Register(CreateSequenceGenerator, Lifetime.Scoped);
            builder.Register<SequenceSession>(Lifetime.Scoped);
            builder.Register<GameplayLoop>(Lifetime.Scoped);
            builder.Register<GameplayProgressTracker>(Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            // A scoped factory keeps controller.Dispose in the scene-scope teardown.
            builder.Register(GetGameplayController, Lifetime.Scoped);

            GameplayController GetGameplayController(IObjectResolver container) => controller;
        }

        private static SequenceGenerator CreateSequenceGenerator(IObjectResolver container)
        {
            return new SequenceGenerator(new System.Random());
        }
    }
}
