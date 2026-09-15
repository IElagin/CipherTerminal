using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.ConfigsManagement
{
    public class ConfigsProviderService
    {
        private readonly Dictionary<Type, object> _configs = new Dictionary<Type, object>();
        private readonly IConfigsLoader[] _loaders;

        public ConfigsProviderService(params IConfigsLoader[] loaders)
        {
            _loaders = loaders;
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            var loaded = new Dictionary<Type, object>();

            foreach (IConfigsLoader loader in _loaders)
            {
                Dictionary<Type, object> configs = await loader.LoadAsync(cancellationToken);
                foreach (KeyValuePair<Type, object> config in configs)
                    loaded.Add(config.Key, config.Value);
            }

            cancellationToken.ThrowIfCancellationRequested();
            _configs.Clear();

            foreach (KeyValuePair<Type, object> config in loaded)
                _configs.Add(config.Key, config.Value);
        }

        public T GetConfig<T>() where T : class
        {
            if (_configs.ContainsKey(typeof(T)) == false)
                throw new InvalidOperationException($"Not found config by {typeof(T)}");

            return (T)_configs[typeof(T)];
        }
    }
}
