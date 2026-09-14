using System;
using System.Collections.Generic;

namespace Assets._Project.Develop.Runtime.Infrastructure.DI
{
    public class DIContainer
    {
        private readonly Dictionary<Type, Registration> _container = new Dictionary<Type, Registration>();
        private readonly List<Type> _requests = new List<Type>();
        private readonly DIContainer _parent;

        public DIContainer() : this(null) { }

        public DIContainer(DIContainer parent) => _parent = parent;

        public bool IsAlreadyRegister<T>()
        {
            if (_container.ContainsKey(typeof(T)))
                return true;

            return _parent != null && _parent.IsAlreadyRegister<T>();
        }

        public void RegisterAsSingle<T>(Func<DIContainer, T> creator)
        {
            if (IsAlreadyRegister<T>())
                throw new InvalidOperationException($"{typeof(T)} already registered");

            var registration = new Registration(container => creator.Invoke(container));
            _container.Add(typeof(T), registration);
        }

        public T Resolve<T>()
        {
            if (_requests.Contains(typeof(T)))
                throw new InvalidOperationException($"Cycle resolve for {typeof(T)}");

            _requests.Add(typeof(T));

            try
            {
                if (_container.TryGetValue(typeof(T), out Registration registration))
                    return (T)registration.CreateInstanceFrom(this);

                if (_parent != null)
                    return _parent.Resolve<T>();
            }
            finally
            {
                _requests.Remove(typeof(T));
            }

            throw new InvalidOperationException($"Registration for {typeof(T)} not found");
        }
    }
}
