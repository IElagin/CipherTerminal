using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.AssetsManagement
{
    public class ResourcesAssetLoader
    {
        public T Load<T>(string path) where T : Object
            => Resources.Load<T>(path);
    }
}
