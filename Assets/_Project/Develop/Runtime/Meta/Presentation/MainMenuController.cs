using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Assets._Project.Develop.Runtime.UI;

namespace Assets._Project.Develop.Runtime.Meta.Presentation
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private TerminalView _view;
        [SerializeField] private TerminalKeyboard _keyboard;

        private SceneNavigator _navigator;
        private IAudioService _audio;
        private bool _running;

        public void Configure(SceneNavigator navigator, IAudioService audio)
        {
            _navigator = navigator;
            _audio = audio;
        }

        public void Run()
        {
            _running = true;
            _keyboard.Character += OnCharacter;
            _view.DigitsButton.onClick.AddListener(OnDigits);
            _view.LettersButton.onClick.AddListener(OnLetters);
            _keyboard.Activate();
            _view.SetMenuEnabled(true);
        }

        private void OnCharacter(char character)
        {
            if (character == '1')
                OnDigits();
            else if (character == '2')
                OnLetters();
        }

        private void OnDigits()
        {
            Choose(SequenceMode.Digits);
        }

        private void OnLetters()
        {
            Choose(SequenceMode.Letters);
        }

        private void Choose(SequenceMode mode)
        {
            if (!_running || _navigator.IsLeaving)
                return;

            _running = false;
            _view.SetMenuEnabled(false);
            _keyboard.Deactivate();
            _audio.Play(AudioCue.Key);
            _navigator.Go(Scenes.Gameplay, new GameplayInputArgs(1, mode));
        }

        private void OnDestroy()
        {
            if (_keyboard != null)
            {
                _keyboard.Deactivate();
                _keyboard.Character -= OnCharacter;
            }

            if (_view != null)
            {
                _view.DigitsButton.onClick.RemoveListener(OnDigits);
                _view.LettersButton.onClick.RemoveListener(OnLetters);
            }
        }
    }
}
