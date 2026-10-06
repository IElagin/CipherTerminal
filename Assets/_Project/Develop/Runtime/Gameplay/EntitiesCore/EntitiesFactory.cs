using Assets._Project.Develop.Runtime.Configs.Gameplay.Enemies;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Hero;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using Assets._Project.Develop.Runtime.Gameplay.Features.Animation;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.Energy;
using Assets._Project.Develop.Runtime.Gameplay.Features.LifeCycle;
using Assets._Project.Develop.Runtime.Gameplay.Features.MovementFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.Teleport;
using Assets._Project.Develop.Runtime.Utilities;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.Layers;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore
{
    public class EntitiesFactory
    {
        private const string TeleporterPrefabPath = "Entities/Teleporter";

        private readonly EntitiesLifeContext _lifeContext;
        private readonly MonoEntitiesFactory _monoEntitiesFactory;
        private readonly CollidersRegistryService _collidersRegistryService;
        private readonly ConfigsProviderService _configsProvider;

        public EntitiesFactory(EntitiesLifeContext lifeContext, MonoEntitiesFactory monoEntitiesFactory,
            CollidersRegistryService collidersRegistryService, ConfigsProviderService configsProvider)
        {
            _lifeContext = lifeContext;
            _monoEntitiesFactory = monoEntitiesFactory;
            _collidersRegistryService = collidersRegistryService;
            _configsProvider = configsProvider;
        }

        public Entity CreateEnemy(EnemyTypes type, Vector3 position)
        {
            EnemyConfig config = _configsProvider.GetConfig<EnemiesListConfig>().GetConfigFor(type);
            Entity entity = CreateMortalMovementEntity(config.MoveSpeed, config.RotationSpeed)
                .AddSystem(new RigidbodyMovementSystem())
                .AddSystem(new RigidbodyRotationSystem());

            AddLifeCycleSystems(entity);
            _monoEntitiesFactory.Create(entity, position, config.PrefabPath);
            _lifeContext.Add(entity);

            return entity;
        }

        public Entity CreateHero(Vector3 position)
        {
            HeroConfig config = _configsProvider.GetConfig<HeroConfig>();
            Entity entity = CreateMortalMovementEntity(config.MoveSpeed, config.RotationSpeed)
                .AddSystem(new CharacterControllerMovementSystem())
                .AddSystem(new TransformRotationSystem())
                .AddSystem(new CharacterAnimationSystem());

            AddLifeCycleSystems(entity);
            _monoEntitiesFactory.Create(entity, position, config.PrefabPath);
            _lifeContext.Add(entity);

            return entity;
        }

        public Entity CreateTeleporter(Vector3 position)
        {
            Entity entity = new Entity();

            AddHealth(entity, 100, 1);

            entity
                .AddMaxEnergy(new ReactiveVariable<float>(100))
                .AddCurrentEnergy(new ReactiveVariable<float>(100))
                .AddEnergyRegenerationPercent(new ReactiveVariable<float>(0.1f))
                .AddEnergyRegenerationInitialTime(new ReactiveVariable<float>(2))
                .AddEnergyRegenerationCurrentTime()
                .AddTeleportRadius(new ReactiveVariable<float>(6))
                .AddTeleportEnergyCost(new ReactiveVariable<float>(25))
                .AddTeleportRequest()
                .AddTeleportEvent()
                .AddAreaDamage(new ReactiveVariable<float>(30))
                .AddAreaDamageRadius(new ReactiveVariable<float>(2.5f))
                .AddAreaDamageMask(UnityLayers.LayerMaskCharacters)
                .AddAreaDamageCollidersBuffer(new Buffer<Collider>(64));

            ICompositeCondition canTeleport = new CompositeCondition()
                .Add(new FuncCondition(() => entity.IsDead.Value == false))
                .Add(new FuncCondition(() => entity.CurrentEnergy.Value >= entity.TeleportEnergyCost.Value));

            entity.AddCanTeleport(canTeleport);

            entity
                .AddSystem(new EnergyRegenerationSystem())
                .AddSystem(new TeleportSystem())
                .AddSystem(new AreaDamageOnTeleportSystem(_collidersRegistryService));

            AddLifeCycleSystems(entity);
            _monoEntitiesFactory.Create(entity, position, TeleporterPrefabPath);
            _lifeContext.Add(entity);

            return entity;
        }

        private Entity CreateMortalMovementEntity(float moveSpeed, float rotationSpeed)
        {
            Entity entity = new Entity()
                .AddMoveDirection()
                .AddMoveSpeed(new ReactiveVariable<float>(moveSpeed))
                .AddIsMoving()
                .AddRotationDirection()
                .AddRotationSpeed(new ReactiveVariable<float>(rotationSpeed));

            AddHealth(entity, 100, 2);

            ICompositeCondition canMove = new CompositeCondition()
                .Add(new FuncCondition(() => entity.IsDead.Value == false));

            ICompositeCondition canRotate = new CompositeCondition()
                .Add(new FuncCondition(() => entity.IsDead.Value == false));

            return entity
                .AddCanMove(canMove)
                .AddCanRotate(canRotate);
        }

        private void AddHealth(Entity entity, float health, float deathProcessTime)
        {
            entity
                .AddMaxHealth(new ReactiveVariable<float>(health))
                .AddCurrentHealth(new ReactiveVariable<float>(health))
                .AddIsDead()
                .AddInDeathProcess()
                .AddDeathProcessInitialTime(new ReactiveVariable<float>(deathProcessTime))
                .AddDeathProcessCurrentTime()
                .AddTakeDamageRequest()
                .AddTakeDamageEvent();

            ICompositeCondition mustDie = new CompositeCondition()
                .Add(new FuncCondition(() => entity.CurrentHealth.Value <= 0));

            ICompositeCondition mustSelfRelease = new CompositeCondition()
                .Add(new FuncCondition(() => entity.IsDead.Value))
                .Add(new FuncCondition(() => entity.InDeathProcess.Value == false));

            ICompositeCondition canApplyDamage = new CompositeCondition()
                .Add(new FuncCondition(() => entity.IsDead.Value == false));

            entity
                .AddMustDie(mustDie)
                .AddMustSelfRelease(mustSelfRelease)
                .AddCanApplyDamage(canApplyDamage);
        }

        private void AddLifeCycleSystems(Entity entity)
        {
            entity
                .AddSystem(new ApplyDamageSystem())
                .AddSystem(new DeathSystem())
                .AddSystem(new DisableCollidersOnDeathSystem())
                .AddSystem(new DeathProcessTimerSystem())
                .AddSystem(new SelfReleaseSystem(_lifeContext));
        }
    }
}
