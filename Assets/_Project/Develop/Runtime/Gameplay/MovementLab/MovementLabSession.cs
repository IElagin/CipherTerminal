using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.MovementLab
{
    public class MovementLabSession
    {
        private static readonly Vector3 _enemySpawn = new(-2, 0, 0);
        private static readonly Vector3 _heroSpawn = new(2, 0, 0);
        private static readonly Vector3 _teleporterSpawn = new(0, 0, 3);
        private const float TestDamage = 50;

        private readonly EntitiesLifeContext _lifeContext;
        private readonly EntitiesFactory _entitiesFactory;
        private readonly MovementLabInput _input;

        private Entity _hero;
        private Entity _enemy;
        private Entity _teleporter;
        private bool _isRunning;

        public MovementLabSession(EntitiesLifeContext lifeContext, EntitiesFactory entitiesFactory, MovementLabInput input)
        {
            _lifeContext = lifeContext;
            _entitiesFactory = entitiesFactory;
            _input = input;
        }

        public void Initialize()
        {
            _enemy = _entitiesFactory.CreateEnemy(EnemyTypes.Basic, _enemySpawn);
            _hero = _entitiesFactory.CreateHero(_heroSpawn);
            _teleporter = _entitiesFactory.CreateTeleporter(_teleporterSpawn);
        }

        public void Run() => _isRunning = true;

        public void Update(float deltaTime)
        {
            if (_isRunning == false)
                return;

            Vector3 direction = _input.ReadDirection();
            SetDirection(_hero, direction);
            SetDirection(_enemy, direction);

            if (_input.IsTeleportPressed())
                _teleporter.TeleportRequest.Invoke();

            if (_input.IsDamagePressed())
                _teleporter.TakeDamageRequest.Invoke(TestDamage);

            _lifeContext.Update(deltaTime);
        }

        public void FixedUpdate(float deltaTime)
        {
            if (_isRunning == false)
                return;

            _lifeContext.FixedUpdate(deltaTime);
        }

        private void SetDirection(Entity entity, Vector3 direction)
        {
            entity.MoveDirection.Value = direction;
            entity.RotationDirection.Value = direction;
        }
    }
}
