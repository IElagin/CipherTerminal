using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Gameplay.Configs;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayContextRegistrations
    {
        public static void Process(DIContainer container, GameplayInputArgs inputArgs)
        {
            container.RegisterAsSingle(c => new SequenceGenerator(new System.Random()));

            container.RegisterAsSingle(c =>
            {
                SequenceConfig config = c.Resolve<ConfigsProviderService>().GetConfig<SequenceConfig>();
                string target = c.Resolve<SequenceGenerator>().Generate(config.GetSymbols(inputArgs.Mode), config.Length);
                return new SequenceSession(target);
            });

            container.RegisterAsSingle(c => new GameplayLoop(c.Resolve<SequenceSession>(), inputArgs));
        }
    }
}
