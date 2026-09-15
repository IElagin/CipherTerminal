using VContainer;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Gameplay.Configs;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayContextRegistrations
    {
        public static void Process(IContainerBuilder builder, GameplayInputArgs inputArgs)
        {
            builder.Register(_ => new SequenceGenerator(new System.Random()), Lifetime.Scoped);

            builder.Register(c =>
            {
                SequenceConfig config = c.Resolve<ConfigsProviderService>().GetConfig<SequenceConfig>();
                string target = c.Resolve<SequenceGenerator>().Generate(config.GetSymbols(inputArgs.Mode), config.Length);
                return new SequenceSession(target);
            }, Lifetime.Scoped);

            builder.Register(c => new GameplayLoop(c.Resolve<SequenceSession>(), inputArgs), Lifetime.Scoped);
            builder.Register<SceneNavigator>(Lifetime.Scoped);
        }
    }
}
