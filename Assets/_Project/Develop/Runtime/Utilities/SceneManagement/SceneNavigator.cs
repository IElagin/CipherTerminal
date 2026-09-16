using System;
using Cysharp.Threading.Tasks;
using Assets._Project.Develop.Runtime.Infrastructure;

namespace Assets._Project.Develop.Runtime.Utilities.SceneManagement
{
    public sealed class SceneNavigator
    {
        private readonly SceneSwitcherService _switcher;

        public SceneNavigator(SceneSwitcherService switcher)
        {
            _switcher = switcher;
        }

        public bool IsLeaving { get; private set; }

        public void Go(string sceneName, IInputSceneArgs sceneArgs = null, Action onFailed = null)
        {
            if (IsLeaving)
                return;

            IsLeaving = true;
            GoAsync(sceneName, sceneArgs, onFailed).Forget(AsyncErrors.Report);
        }

        private async UniTask GoAsync(string sceneName, IInputSceneArgs sceneArgs, Action onFailed)
        {
            try
            {
                await _switcher.SwitchAsync(sceneName, sceneArgs);
            }
            catch
            {
                IsLeaving = false;
                onFailed?.Invoke();
                throw;
            }
        }
    }
}
