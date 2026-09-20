using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.UI.ResetStatistics;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.UI.MainMenu
{
    public sealed class MainMenuScreenPresenter : IPresenter
    {
        private const int InitialLevelNumber = 1;

        private readonly MainMenuScreenView _view;
        private readonly TerminalKeyboard _keyboard;
        private readonly SceneNavigator _navigator;
        private readonly IAudioService _audio;
        private readonly PlayerProgressService _progress;
        private readonly MainMenuPopupService _popups;
        private readonly ProjectPresentersFactory _factory;

        private ProgressPanelPresenter _panel;
        private ResetStatisticsPopupPresenter _activePopup;
        private bool _running;

        public MainMenuScreenPresenter(MainMenuScreenView view, TerminalKeyboard keyboard,
            SceneNavigator navigator, IAudioService audio, PlayerProgressService progress,
            MainMenuPopupService popups, ProjectPresentersFactory factory)
        {
            _view = view;
            _keyboard = keyboard;
            _navigator = navigator;
            _audio = audio;
            _progress = progress;
            _popups = popups;
            _factory = factory;
        }

        public void Initialize()
        {
            _panel = _factory.CreateProgressPanelPresenter(_view.ProgressPanel);
            _panel.Initialize();
            _panel.ResetRequested += OpenReset;
            _view.DigitsClicked += OnDigits;
            _view.LettersClicked += OnLetters;
            RefreshInteraction();
        }

        public void Run()
        {
            _running = true;
            _keyboard.CharacterEntered += OnCharacterEntered;
            _keyboard.EscapePressed += OnEscape;
            _keyboard.Activate();
            RefreshInteraction();
        }

        public void Dispose()
        {
            _running = false;
            _keyboard.Deactivate();
            _keyboard.CharacterEntered -= OnCharacterEntered;
            _keyboard.EscapePressed -= OnEscape;
            _view.DigitsClicked -= OnDigits;
            _view.LettersClicked -= OnLetters;
            _panel.ResetRequested -= OpenReset;
            _panel.Dispose();
        }

        private void OnCharacterEntered(char character)
        {
            if (_activePopup != null)
                return;

            if (character == '1')
                OnDigits();
            else if (character == '2')
                OnLetters();
            else if (character == '4')
                OpenReset();
        }

        private void OnDigits() => Choose(SequenceMode.Digits);

        private void OnLetters() => Choose(SequenceMode.Letters);

        private void OnEscape() => _activePopup?.RequestCancel();

        private void Choose(SequenceMode mode)
        {
            if (_running == false || _activePopup != null || _progress.IsReady == false || _navigator.IsLeaving)
                return;

            _running = false;
            RefreshInteraction();
            _keyboard.Deactivate();
            _audio.Play(AudioCue.Key);
            _navigator.Go(Scenes.Gameplay, new GameplayInputArgs(InitialLevelNumber, mode));
        }

        private void OpenReset()
        {
            if (_running == false || _activePopup != null || _progress.IsReady == false)
                return;

            _view.SetInteractable(false);
            _panel.SetInteractable(false);
            _activePopup = _popups.OpenResetStatisticsPopup(OnPopupClosed);
            _audio.Play(AudioCue.Key);
        }

        private void OnPopupClosed()
        {
            _activePopup = null;
            RefreshInteraction();
        }

        private void RefreshInteraction()
        {
            bool enabled = _running && _progress.IsReady && _activePopup == null;
            _view.SetInteractable(enabled);
            _panel.SetInteractable(enabled);
        }
    }
}
