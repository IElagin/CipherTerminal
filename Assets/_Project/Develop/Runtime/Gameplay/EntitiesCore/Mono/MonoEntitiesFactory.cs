using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono
{
    public class MonoEntitiesFactory : IDisposable
    {
        private readonly ResourcesAssetLoader _assetsLoader;
        private readonly EntitiesLifeContext _lifeContext;
        private readonly CollidersRegistryService _collidersRegistryService;
        private readonly Dictionary<Entity, MonoEntity> _views = new();

        public MonoEntitiesFactory(ResourcesAssetLoader assetsLoader, EntitiesLifeContext lifeContext,
            CollidersRegistryService collidersRegistryService)
        {
            _assetsLoader = assetsLoader;
            _lifeContext = lifeContext;
            _collidersRegistryService = collidersRegistryService;
        }

        public void Initialize() => _lifeContext.Released += OnEntityReleased;

        public MonoEntity Create(Entity entity, Vector3 position, string path)
        {
            MonoEntity prefab = _assetsLoader.Load<MonoEntity>(path);
            MonoEntity view = Object.Instantiate(prefab, position, Quaternion.identity);
            view.Initialize(_collidersRegistryService);
            view.Link(entity);
            _views.Add(entity, view);

            return view;
        }

        public void Dispose()
        {
            _lifeContext.Released -= OnEntityReleased;

            foreach (KeyValuePair<Entity, MonoEntity> pair in _views)
                CleanupFor(pair.Key, pair.Value);

            _views.Clear();
        }

        private void OnEntityReleased(Entity entity)
        {
            if (_views.TryGetValue(entity, out MonoEntity view) == false)
                return;

            CleanupFor(entity, view);
            _views.Remove(entity);
        }

        private void CleanupFor(Entity entity, MonoEntity view)
        {
            // On Play Mode exit Unity can destroy the scene before the project scope.
            if (view == null)
                return;

            view.Cleanup(entity);
            Object.Destroy(view.gameObject);
        }
    }
}
