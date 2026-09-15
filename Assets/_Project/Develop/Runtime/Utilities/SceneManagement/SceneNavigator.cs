using System;
using Cysharp.Threading.Tasks;
using Assets._Project.Develop.Runtime.Infrastructure;

namespace Assets._Project.Develop.Runtime.Utilities.SceneManagement
{
    public sealed class SceneNavigator
    {
        private readonly SceneSwitcherService _switcher;

        public bool IsLeaving { get; private set; }

        public SceneNavigator(SceneSwitcherService switcher)
        {
            _switcher = switcher;
        }

        public void Go(string scene, IInputSceneArgs args = null, Action onFailed = null)
        {
            if (IsLeaving)
                return;

            IsLeaving = true;
            GoAsync(scene, args, onFailed).Forget(AsyncErrors.Report);
        }

        private async UniTask GoAsync(string scene, IInputSceneArgs args, Action onFailed)
        {
            try
            {
                await _switcher.SwitchAsync(scene, args);
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
