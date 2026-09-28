using System;
using System.Collections.Generic;

namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore
{
    public class EntitiesLifeContext : IDisposable
    {
        private readonly List<Entity> _entities = new();
        private readonly List<Entity> _releaseRequests = new();

        public event Action<Entity> Added;
        public event Action<Entity> Released;

        public void Add(Entity entity)
        {
            _entities.Add(entity);
            entity.Initialize();
            Added?.Invoke(entity);
        }

        public void Release(Entity entity) => _releaseRequests.Add(entity);

        public void Update(float deltaTime)
        {
            for (int index = 0; index < _entities.Count; index++)
                _entities[index].Update(deltaTime);

            for (int index = 0; index < _releaseRequests.Count; index++)
            {
                Entity entity = _releaseRequests[index];

                if (_entities.Remove(entity) == false)
                    continue;

                entity.Dispose();
                Released?.Invoke(entity);
            }

            _releaseRequests.Clear();
        }

        public void FixedUpdate(float deltaTime)
        {
            for (int index = 0; index < _entities.Count; index++)
                _entities[index].FixedUpdate(deltaTime);
        }

        public void Dispose()
        {
            foreach (Entity entity in _entities)
                entity.Dispose();

            _entities.Clear();
            _releaseRequests.Clear();
        }
    }
}
