using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Utilities.LoadingScreen;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets._Project.Develop.Runtime.Utilities.SceneManagement
{
    public class SceneSwitcherService : IDisposable
    {
        private readonly SceneLoaderService _sceneLoaderService;
        private readonly ILoadingScreen _loadingScreen;
        private readonly IObjectResolver _projectContainer;
        private readonly CancellationToken _projectToken;

        private IScopedObjectResolver _sceneContainer;
        private CancellationTokenSource _sceneLifetime;
        private bool _isDisposed;

        public SceneSwitcherService(SceneLoaderService sceneLoaderService, ILoadingScreen loadingScreen,
            IObjectResolver projectContainer, CancellationToken projectToken)
        {
            _sceneLoaderService = sceneLoaderService;
            _loadingScreen = loadingScreen;
            _projectContainer = projectContainer;
            _projectToken = projectToken;
        }

        public bool IsSwitching { get; private set; }

        public async UniTask SwitchAsync(string sceneName, IInputSceneArgs sceneArgs = null,
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SceneSwitcherService));

            _projectToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();

            if (IsSwitching)
                throw new InvalidOperationException("A scene transition is already running");

            ValidateDestination(sceneName, sceneArgs);
            IsSwitching = true;

            using var transitionLifetime =
                CancellationTokenSource.CreateLinkedTokenSource(_projectToken, cancellationToken);
            CancellationToken token = transitionLifetime.Token;

            try
            {
                _loadingScreen.Show();
                ReleaseScene();

                await _sceneLoaderService.LoadAsync(Scenes.Empty, cancellationToken: token);
                ThrowIfStopped(token);

                await _sceneLoaderService.LoadAsync(sceneName, cancellationToken: token);
                ThrowIfStopped(token);

                SceneBootstrap bootstrap = FindBootstrap(SceneManager.GetActiveScene());
                _sceneLifetime = CancellationTokenSource.CreateLinkedTokenSource(_projectToken);
                _sceneContainer = _projectContainer.CreateScope(
                    builder => bootstrap.ProcessRegistrations(builder, sceneArgs));

                using (token.Register(_sceneLifetime.Cancel))
                    await bootstrap.Initialize(_sceneContainer, _sceneLifetime.Token);

                ThrowIfStopped(token);
                _loadingScreen.Hide();
                bootstrap.Run();
            }
            catch (OperationCanceledException)
            {
                ReleaseScene();
                throw;
            }
            finally
            {
                if (_isDisposed == false)
                    _loadingScreen.Hide();

                IsSwitching = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            ReleaseScene();
        }

        private void ReleaseScene()
        {
            _sceneLifetime?.Cancel();
            _sceneContainer?.Dispose();
            _sceneContainer = null;

            _sceneLifetime?.Dispose();
            _sceneLifetime = null;
        }

        private void ThrowIfStopped(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SceneSwitcherService));
        }

        private static void ValidateDestination(string sceneName, IInputSceneArgs sceneArgs)
        {
            if (sceneName == Scenes.Empty || sceneName == Scenes.GameEntryPoint ||
                Application.CanStreamedLevelBeLoaded(sceneName) == false)
            {
                throw new ArgumentException($"Invalid destination scene: {sceneName}", nameof(sceneName));
            }

            if (sceneName != Scenes.Gameplay)
                return;

            GameplayInputArgs gameplayInputArgs = (GameplayInputArgs)sceneArgs;

            gameplayInputArgs.Validate(nameof(sceneArgs));
        }

        private static SceneBootstrap FindBootstrap(Scene scene)
        {
            SceneBootstrap result = null;

            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SceneBootstrap candidate in root.GetComponentsInChildren<SceneBootstrap>())
                {
                    if (result != null)
                        throw new InvalidOperationException($"Multiple bootstraps in {scene.name}");

                    result = candidate;
                }

            if (result == null)
                throw new InvalidOperationException($"SceneBootstrap not found in {scene.name}");

            return result;
        }
    }
}
