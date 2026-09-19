using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Assets._Project.Develop.Runtime.UI;

namespace Assets._Project.Develop.Runtime.Meta.Presentation
{
    public sealed class MainMenuController : MonoBehaviour, IDisposable
    {
        private const int InitialLevelNumber = 1;

        [SerializeField] private TerminalView _view;
        [SerializeField] private TerminalKeyboard _keyboard;
        [SerializeField] private WalletPanelView _walletView;

        private SceneNavigator _navigator;
        private IAudioService _audio;
        private PlayerProgressService _progress;
        private bool _running;
        private bool _disposed;

        public void Initialize(SceneNavigator navigator, IAudioService audio, PlayerProgressService progress)
        {
            _navigator = navigator;
            _audio = audio;
            _progress = progress;
        }

        public void Run()
        {
            if (_disposed || _running)
                return;

            _running = true;
            _keyboard.CharacterEntered += OnCharacterEntered;
            _keyboard.EscapePressed += OnEscapePressed;
            _view.DigitsButton.onClick.AddListener(OnDigits);
            _view.LettersButton.onClick.AddListener(OnLetters);
            _walletView.ToggleButton.onClick.AddListener(OnToggleStatistics);
            _walletView.ResetButton.onClick.AddListener(OnResetStatistics);
            _progress.Changed += OnProgressChanged;
            _keyboard.Activate();
            _walletView.SetExpanded(false);
            RefreshProgress();
        }

        private void OnCharacterEntered(char character)
        {
            if (character == '1')
                OnDigits();
            else if (character == '2')
                OnLetters();
            else if (character == '3')
                OnToggleStatistics();
            else if (character == '4' && _walletView.IsExpanded)
                OnResetStatistics();
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
            if (_running == false || _navigator.IsLeaving || _progress.IsReady == false)
                return;

            _running = false;
            _view.SetMenuEnabled(false);
            _walletView.SetInteractable(false);
            _keyboard.Deactivate();
            _audio.Play(AudioCue.Key);
            _navigator.Go(Scenes.Gameplay, new GameplayInputArgs(InitialLevelNumber, mode));
        }

        private void OnToggleStatistics()
        {
            if (_running == false || _progress.IsReady == false)
                return;

            bool expanded = _walletView.IsExpanded == false;
            _walletView.SetExpanded(expanded);
            _audio.Play(AudioCue.Key);

            if (expanded)
            {
                ProgressSnapshot snapshot = _progress.Snapshot;
                string report = "Победы: " + snapshot.Wins + " · Поражения: " + snapshot.Losses +
                                " · Золото: " + snapshot.Gold;
                Debug.Log(report);

                if (string.IsNullOrEmpty(_progress.Error))
                    _walletView.ShowStatus(report);

                return;
            }

            ShowPersistentStatus();
        }

        private void OnEscapePressed()
        {
            if (_running == false || _walletView.IsExpanded == false)
                return;

            _walletView.SetExpanded(false);
            _audio.Play(AudioCue.Key);
            ShowPersistentStatus();
        }

        private void OnResetStatistics()
        {
            if (_running == false || _walletView.IsExpanded == false || _progress.IsReady == false)
                return;

            ResetStatisticsAsync().Forget(AsyncErrors.Report);
        }

        private async UniTask ResetStatisticsAsync()
        {
            int availableGold = _progress.Snapshot.Gold;
            _view.SetMenuEnabled(false);
            _walletView.SetInteractable(false);

            ProgressOperationResult result = await _progress.ResetStatisticsAsync();

            if (this == null || _disposed)
                return;

            RefreshInteractability();

            switch (result.Status)
            {
                case ProgressOperationStatus.Completed:
                    string success = "Статистика сброшена · −" +
                                     _progress.StatisticsResetCost + " золота";
                    _audio.Play(AudioCue.Key);
                    Debug.Log(success);
                    _walletView.ShowStatus(success);
                    break;

                case ProgressOperationStatus.SavedInMemoryOnly:
                    _audio.Play(AudioCue.Error);
                    Debug.LogError(_progress.Error);
                    _walletView.ShowStatus(_progress.Error, true);
                    break;

                case ProgressOperationStatus.InsufficientGold:
                    string insufficient = "Недостаточно золота: нужно " +
                                          _progress.StatisticsResetCost + ", есть " + availableGold;
                    _audio.Play(AudioCue.Error);
                    Debug.Log(insufficient);
                    _walletView.ShowStatus(insufficient, true);
                    break;

                default:
                    _audio.Play(AudioCue.Error);
                    ShowPersistentStatus();
                    break;
            }
        }

        private void OnProgressChanged(ProgressSnapshot snapshot)
        {
            if (this == null || _disposed)
                return;

            _walletView.Render(snapshot.Gold, snapshot.Wins, snapshot.Losses,
                _progress.StatisticsResetCost);
            RefreshInteractability();
            ShowPersistentStatus();
        }

        private void RefreshProgress()
        {
            if (_progress.IsReady == false)
            {
                _walletView.ShowUnavailable(_progress.Error ?? "Прогресс недоступен");
                RefreshInteractability();
                return;
            }

            ProgressSnapshot snapshot = _progress.Snapshot;
            _walletView.Render(snapshot.Gold, snapshot.Wins, snapshot.Losses,
                _progress.StatisticsResetCost);
            RefreshInteractability();
            ShowPersistentStatus();
        }

        private void RefreshInteractability()
        {
            bool interactable = _running && _progress.IsReady;
            _view.SetMenuEnabled(interactable);
            _walletView.SetInteractable(interactable);
        }

        private void ShowPersistentStatus()
        {
            if (string.IsNullOrEmpty(_progress.Error))
                _walletView.ShowStatus("> Система готова");
            else
                _walletView.ShowStatus(_progress.Error, true);
        }

        private void Stop()
        {
            _running = false;

            if (_keyboard != null)
            {
                _keyboard.Deactivate();
                _keyboard.CharacterEntered -= OnCharacterEntered;
                _keyboard.EscapePressed -= OnEscapePressed;
            }

            if (_view != null)
            {
                _view.DigitsButton.onClick.RemoveListener(OnDigits);
                _view.LettersButton.onClick.RemoveListener(OnLetters);
            }

            if (_walletView != null)
            {
                _walletView.ToggleButton.onClick.RemoveListener(OnToggleStatistics);
                _walletView.ResetButton.onClick.RemoveListener(OnResetStatistics);
            }

            if (_progress != null)
                _progress.Changed -= OnProgressChanged;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
