using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.LoadingScreen;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Infrastructure.EntryPoint
{
    public class GameEntryPoint : MonoBehaviour
    {
        private static GameEntryPoint _instance;

        private IObjectResolver _projectContainer;
        private CancellationTokenSource _projectLifetime;
        private StandardLoadingScreen _loadingScreen;
        private AudioService _audioService;
        private SceneSwitcherService _sceneSwitcher;

        public async UniTask Initialize(CancellationToken cancellationToken)
        {
            Debug.Log("Start project: setup settings");
            SetupAppSettings();

            StandardLoadingScreen loadingPrefab =
                Resources.Load<StandardLoadingScreen>("Utilities/StandardLoadingScreen");

            if (loadingPrefab == null)
                throw new InvalidOperationException("Loading screen prefab not found");

            AudioService audioPrefab = Resources.Load<AudioService>("Utilities/AudioService");

            if (audioPrefab == null)
                throw new InvalidOperationException("Audio service prefab not found");

            _loadingScreen = Instantiate(loadingPrefab);
            _audioService = Instantiate(audioPrefab);
            _loadingScreen.Show();

            var builder = new ContainerBuilder();
            ProjectContextRegistrations.Process(builder, _loadingScreen, _audioService, cancellationToken);
            _projectContainer = builder.Build();
            _sceneSwitcher = _projectContainer.Resolve<SceneSwitcherService>();

            Debug.Log("Initialize project services");
            ConfigsProviderService configs = _projectContainer.Resolve<ConfigsProviderService>();
            await configs.LoadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _audioService.Initialize(configs.GetConfig<AudioCatalog>());
            cancellationToken.ThrowIfCancellationRequested();

            PlayerProgressService progress = _projectContainer.Resolve<PlayerProgressService>();
            await progress.Initialize(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("Project services initialized");

            await _sceneSwitcher.SwitchAsync(Scenes.MainMenu);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _projectLifetime = new CancellationTokenSource();
            Initialize(_projectLifetime.Token).Forget(AsyncErrors.Report);
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            _instance = null;
            ReleaseServices();
        }

        private void ReleaseServices()
        {
            _projectLifetime?.Cancel();

            _sceneSwitcher?.Dispose();
            _sceneSwitcher = null;

            _projectContainer?.Dispose();
            _projectContainer = null;

            _projectLifetime?.Dispose();
            _projectLifetime = null;

            if (_audioService != null)
                Destroy(_audioService.gameObject);

            _audioService = null;

            if (_loadingScreen != null)
                Destroy(_loadingScreen.gameObject);

            _loadingScreen = null;
        }

        private void SetupAppSettings()
        {
            const int targetFrameRate = 60;
            const int minimumVSyncCount = 1;
            const int maximumVSyncCount = 4;

            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = false;

#if UNITY_EDITOR
            // Game View VSync must also be enabled in the Editor window.
            int refreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            QualitySettings.vSyncCount = Mathf.Clamp(
                Mathf.CeilToInt(refreshRate / (float)targetFrameRate), minimumVSyncCount, maximumVSyncCount);
#else
            QualitySettings.vSyncCount = 0;
#endif
        }
    }
}
