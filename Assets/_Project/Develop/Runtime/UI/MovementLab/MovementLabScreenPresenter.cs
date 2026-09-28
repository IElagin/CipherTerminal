using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using UnityEngine.InputSystem;

namespace Assets._Project.Develop.Runtime.UI.MovementLab
{
    public sealed class MovementLabScreenPresenter : IPresenter
    {
        private readonly MovementLabScreenView _view;
        private readonly SceneNavigator _navigator;

        public MovementLabScreenPresenter(MovementLabScreenView view, SceneNavigator navigator)
        {
            _view = view;
            _navigator = navigator;
        }

        public void Initialize() => _view.BackClicked += GoBack;

        public void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                GoBack();
        }

        public void Dispose() => _view.BackClicked -= GoBack;

        private void GoBack() => _navigator.Go(Scenes.MainMenu);
    }
}
