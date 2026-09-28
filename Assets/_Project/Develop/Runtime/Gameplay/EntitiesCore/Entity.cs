using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;

namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore
{
    public partial class Entity : IDisposable
    {
        private readonly Dictionary<Type, IEntityComponent> _components = new();
        private readonly List<IEntitySystem> _systems = new();
        private readonly List<IInitializableSystem> _initializableSystems = new();
        private readonly List<IUpdatableSystem> _updatableSystems = new();
        private readonly List<IFixedUpdatableSystem> _fixedUpdatableSystems = new();
        private readonly List<IDisposableSystem> _disposableSystems = new();

        private bool _isInitialized;

        public Entity AddComponent<T>(T component) where T : class, IEntityComponent
        {
            _components.Add(typeof(T), component);

            return this;
        }

        public bool TryGetComponent<T>(out T component) where T : class, IEntityComponent
        {
            if (_components.TryGetValue(typeof(T), out IEntityComponent value))
            {
                component = (T)value;

                return true;
            }

            component = default;

            return false;
        }

        public T GetComponent<T>() where T : class, IEntityComponent
        {
            if (TryGetComponent(out T component))
                return component;

            throw new ArgumentException($"Entity has no required component {typeof(T).FullName}");
        }

        public bool HasComponent<T>() where T : class, IEntityComponent => _components.ContainsKey(typeof(T));

        public Entity AddSystem(IEntitySystem system)
        {
            if (_systems.Contains(system))
                throw new InvalidOperationException("The system instance is already attached to this entity");

            _systems.Add(system);

            if (system is IInitializableSystem initializable)
                _initializableSystems.Add(initializable);

            if (system is IUpdatableSystem updatable)
                _updatableSystems.Add(updatable);

            if (system is IFixedUpdatableSystem fixedUpdatable)
                _fixedUpdatableSystems.Add(fixedUpdatable);

            if (system is IDisposableSystem disposable)
                _disposableSystems.Add(disposable);

            if (_isInitialized && system is IInitializableSystem lateInitializable)
                lateInitializable.OnInit(this);

            return this;
        }

        public void Initialize()
        {
            foreach (IInitializableSystem system in _initializableSystems)
                system.OnInit(this);

            _isInitialized = true;
        }

        public void Update(float deltaTime)
        {
            if (_isInitialized == false)
                return;

            foreach (IUpdatableSystem system in _updatableSystems)
                system.OnUpdate(deltaTime);
        }

        public void FixedUpdate(float deltaTime)
        {
            if (_isInitialized == false)
                return;

            foreach (IFixedUpdatableSystem system in _fixedUpdatableSystems)
                system.OnFixedUpdate(deltaTime);
        }

        public void Dispose()
        {
            _isInitialized = false;

            foreach (IDisposableSystem system in _disposableSystems)
                system.OnDispose();
        }
    }
}
