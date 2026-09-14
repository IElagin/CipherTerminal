using System;
using System.Collections;
using System.Collections.Generic;
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

        public IEnumerator LoadAsync(Action<Dictionary<Type, object>> onConfigsLoaded)
        {
            var loadedConfigs = new Dictionary<Type, object>();

            foreach (KeyValuePair<Type, string> configResourcesPath in _configsResourcesPaths)
            {
                ScriptableObject config = _resources.Load<ScriptableObject>(configResourcesPath.Value);
                loadedConfigs.Add(configResourcesPath.Key, config);
                yield return null;
            }

            onConfigsLoaded(loadedConfigs);
        }
    }
}
