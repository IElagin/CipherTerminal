using Assets._Project.Develop.Runtime.Configs.Gameplay.Enemies;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Hero;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore
{
    public class EntitiesFactory
    {
        private readonly EntitiesLifeContext _lifeContext;
        private readonly MonoEntitiesFactory _monoEntitiesFactory;
        private readonly ConfigsProviderService _configsProvider;

        public EntitiesFactory(EntitiesLifeContext lifeContext, MonoEntitiesFactory monoEntitiesFactory,
            ConfigsProviderService configsProvider)
        {
            _lifeContext = lifeContext;
            _monoEntitiesFactory = monoEntitiesFactory;
            _configsProvider = configsProvider;
        }

        public Entity CreateEnemy(EnemyTypes type, Vector3 position)
        {
            EnemyConfig config = _configsProvider.GetConfig<EnemiesListConfig>().GetConfigFor(type);
            Entity entity = CreateMovementEntity(config.MoveSpeed, config.RotationSpeed)
                .AddSystem(new RigidbodyMovementSystem())
                .AddSystem(new RigidbodyRotationSystem());

            _monoEntitiesFactory.Create(entity, position, config.PrefabPath);
            _lifeContext.Add(entity);

            return entity;
        }

        public Entity CreateHero(Vector3 position)
        {
            HeroConfig config = _configsProvider.GetConfig<HeroConfig>();
            Entity entity = CreateMovementEntity(config.MoveSpeed, config.RotationSpeed)
                .AddSystem(new CharacterControllerMovementSystem())
                .AddSystem(new TransformRotationSystem());

            _monoEntitiesFactory.Create(entity, position, config.PrefabPath);
            _lifeContext.Add(entity);

            return entity;
        }

        private Entity CreateMovementEntity(float moveSpeed, float rotationSpeed)
        {
            return new Entity()
                .AddMoveDirection()
                .AddMoveSpeed(new ReactiveVariable<float>(moveSpeed))
                .AddRotationDirection()
                .AddRotationSpeed(new ReactiveVariable<float>(rotationSpeed));
        }
    }
}
