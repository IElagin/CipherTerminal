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

        public void Go(string sceneName, IInputSceneArgs sceneArgs = null)
        {
            if (IsLeaving)
                return;

            IsLeaving = true;
            _switcher.SwitchAsync(sceneName, sceneArgs).Forget(AsyncErrors.Report);
        }
    }
}
