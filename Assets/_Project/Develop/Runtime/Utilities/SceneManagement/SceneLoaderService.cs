using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets._Project.Develop.Runtime.Utilities.SceneManagement
{
    public class SceneLoaderService
    {
        public async UniTask LoadAsync(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

            if (operation == null)
                throw new InvalidOperationException($"Cannot load scene: {sceneName}");

            await operation;
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async UniTask UnloadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

            if (operation == null)
                throw new InvalidOperationException($"Cannot unload scene: {sceneName}");

            await operation;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
