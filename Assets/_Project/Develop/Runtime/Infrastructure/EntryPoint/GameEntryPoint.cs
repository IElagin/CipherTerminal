using System.Collections;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagement;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagement;
using Assets._Project.Develop.Runtime.Utilities.LoadingScreen;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Infrastructure.EntryPoint
{
    public class GameEntryPoint : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("Start project: setup settings");
            SetupAppSettings();

            var projectContainer = new DIContainer();
            Debug.Log("Register project services");
            ProjectContextRegistrations.Process(projectContainer);
            projectContainer.Resolve<ICoroutinesPerformer>().StartPerform(Initialize(projectContainer));
        }

        private IEnumerator Initialize(DIContainer projectContainer)
        {
            ILoadingScreen loadingScreen = projectContainer.Resolve<ILoadingScreen>();
            loadingScreen.Show();
            Debug.Log("Initialize project services");
            yield return projectContainer.Resolve<ConfigsProviderService>().LoadAsync();
            ConfigsProviderService configs = projectContainer.Resolve<ConfigsProviderService>();
            projectContainer.Resolve<IAudioService>().Initialize(configs.GetConfig<AudioCatalog>());
            Debug.Log("Project services initialized");
            loadingScreen.Hide();
            yield return projectContainer.Resolve<SceneSwitcherService>().ProcessSwitchTo(Scenes.MainMenu);
        }

        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
