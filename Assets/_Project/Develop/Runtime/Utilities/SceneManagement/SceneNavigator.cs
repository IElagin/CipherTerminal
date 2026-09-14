using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagement;

namespace Assets._Project.Develop.Runtime.Utilities.SceneManagement
{
    public sealed class SceneNavigator
    {
        private readonly SceneSwitcherService _switcher;
        private readonly ICoroutinesPerformer _coroutines;

        public bool IsLeaving { get; private set; }

        public SceneNavigator(SceneSwitcherService switcher, ICoroutinesPerformer coroutines)
        {
            _switcher = switcher;
            _coroutines = coroutines;
        }

        public void Go(string scene, IInputSceneArgs args = null)
        {
            if (IsLeaving)
                return;

            IsLeaving = true;
            _coroutines.StartPerform(_switcher.ProcessSwitchTo(scene, args));
        }
    }
}
