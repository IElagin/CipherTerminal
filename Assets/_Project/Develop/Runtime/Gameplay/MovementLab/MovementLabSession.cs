using System;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.MovementLab
{
    public class MovementLabSession : IDisposable
    {
        private static readonly Vector3 _enemySpawn = new(-2, 0, 0);
        private static readonly Vector3 _heroSpawn = new(2, 0, 0);

        private readonly EntitiesLifeContext _lifeContext;
        private readonly MonoEntitiesFactory _monoEntitiesFactory;
        private readonly EntitiesFactory _entitiesFactory;
        private readonly MovementLabInput _input;

        private Entity _hero;
        private Entity _enemy;
        private bool _isRunning;

        public MovementLabSession(EntitiesLifeContext lifeContext, MonoEntitiesFactory monoEntitiesFactory,
            EntitiesFactory entitiesFactory, MovementLabInput input)
        {
            _lifeContext = lifeContext;
            _monoEntitiesFactory = monoEntitiesFactory;
            _entitiesFactory = entitiesFactory;
            _input = input;
        }

        public void Initialize()
        {
            _monoEntitiesFactory.Initialize();
            _enemy = _entitiesFactory.CreateEnemy(EnemyTypes.Basic, _enemySpawn);
            _hero = _entitiesFactory.CreateHero(_heroSpawn);
        }

        public void Run() => _isRunning = true;

        public void Update(float deltaTime)
        {
            if (_isRunning == false)
                return;

            Vector3 direction = _input.ReadDirection();
            SetDirection(_hero, direction);
            SetDirection(_enemy, direction);
            _lifeContext.Update(deltaTime);
        }

        public void FixedUpdate(float deltaTime)
        {
            if (_isRunning == false)
                return;

            _lifeContext.FixedUpdate(deltaTime);
        }

        public void Dispose()
        {
            _isRunning = false;
            _lifeContext.Dispose();
            _monoEntitiesFactory.Dispose();
        }

        private void SetDirection(Entity entity, Vector3 direction)
        {
            entity.MoveDirection.Value = direction;
            entity.RotationDirection.Value = direction;
        }
    }
}
