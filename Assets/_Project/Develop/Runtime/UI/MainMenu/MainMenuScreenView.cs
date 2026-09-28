using System;
using Assets._Project.Develop.Runtime.UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.MainMenu
{
    public sealed class MainMenuScreenView : MonoBehaviour, IView
    {
        [SerializeField] private Button _digitsButton;
        [SerializeField] private Button _lettersButton;
        [SerializeField] private Button _movementLabButton;
        [SerializeField] private ProgressPanelView _progressPanel;

        public event Action DigitsClicked;

        public event Action LettersClicked;

        public event Action MovementLabClicked;

        public ProgressPanelView ProgressPanel => _progressPanel;

        public void SetInteractable(bool enabled)
        {
            _digitsButton.interactable = enabled;
            _lettersButton.interactable = enabled;
            _movementLabButton.interactable = enabled;
        }

        private void OnEnable()
        {
            _digitsButton.onClick.AddListener(OnDigitsClicked);
            _lettersButton.onClick.AddListener(OnLettersClicked);
            _movementLabButton.onClick.AddListener(OnMovementLabClicked);
        }

        private void OnDisable()
        {
            _digitsButton.onClick.RemoveListener(OnDigitsClicked);
            _lettersButton.onClick.RemoveListener(OnLettersClicked);
            _movementLabButton.onClick.RemoveListener(OnMovementLabClicked);
        }

        private void OnDigitsClicked() => DigitsClicked?.Invoke();

        private void OnMovementLabClicked() => MovementLabClicked?.Invoke();

        private void OnLettersClicked() => LettersClicked?.Invoke();
    }
}
