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
        [SerializeField] private ProjectLifetimeScope _projectScope;

        public async UniTask Initialize()
        {
            Debug.Log("Start project: setup settings");
            SetupAppSettings();
            _projectScope.Initialize();
            IObjectResolver projectContainer = _projectScope.Container;
            CancellationToken cancellationToken = _projectScope.Token;
            projectContainer.Resolve<ILoadingScreen>().Show();

            Debug.Log("Initialize project services");
            ConfigsProviderService configs = projectContainer.Resolve<ConfigsProviderService>();
            await configs.LoadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            projectContainer.Resolve<IAudioService>().Initialize(configs.GetConfig<AudioCatalog>());
            cancellationToken.ThrowIfCancellationRequested();

            PlayerProgressService progress = projectContainer.Resolve<PlayerProgressService>();
            await progress.Initialize(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("Project services initialized");

            await projectContainer.Resolve<SceneSwitcherService>().SwitchAsync(Scenes.MainMenu);
        }

        private void Awake()
        {
            Initialize().Forget(AsyncErrors.Report);
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
