using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;

namespace Assets._Project.Develop.Runtime.Utilities.ConfigsManagement
{
    public class ResourcesConfigsLoader : IConfigsLoader
    {
        private readonly ResourcesAssetLoader _resources;
        private readonly Dictionary<Type, string> _configsResourcesPaths = new Dictionary<Type, string>();

        public ResourcesConfigsLoader(ResourcesAssetLoader resources)
        {
            _resources = resources;
            _configsResourcesPaths.Add(typeof(Assets._Project.Develop.Runtime.Gameplay.Configs.SequenceConfig), "Configs/SequenceConfig");
            _configsResourcesPaths.Add(typeof(Assets._Project.Develop.Runtime.Utilities.Audio.AudioCatalog), "Configs/AudioCatalog");
        }

        public async UniTask<Dictionary<Type, object>> LoadAsync(CancellationToken cancellationToken = default)
        {
            var loadedConfigs = new Dictionary<Type, object>();

            foreach (KeyValuePair<Type, string> configResourcesPath in _configsResourcesPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ScriptableObject config = _resources.Load<ScriptableObject>(configResourcesPath.Value);
                if (config == null)
                    throw new InvalidOperationException($"Missing config: {configResourcesPath.Value}");

                loadedConfigs.Add(configResourcesPath.Key, config);
                await UniTask.NextFrame(cancellationToken: cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return loadedConfigs;
        }
    }
}
