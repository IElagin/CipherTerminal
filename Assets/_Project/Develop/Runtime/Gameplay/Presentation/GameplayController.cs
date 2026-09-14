using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;
using Assets._Project.Develop.Runtime.UI;

namespace Assets._Project.Develop.Runtime.Gameplay.Presentation
{
    public sealed class GameplayController : MonoBehaviour
    {
        [SerializeField] private TerminalView _view;
        [SerializeField] private TerminalKeyboard _keyboard;

        private GameplayLoop _loop;
        private SceneNavigator _navigator;
        private IAudioService _audio;
        private bool _running;
        private bool _subscribed;
        private bool _initialUpdateReceived;

        public SequenceSession Session => _loop?.Session;

        public void Configure(GameplayLoop loop, SceneNavigator navigator, IAudioService audio)
        {
            _loop = loop;
            _navigator = navigator;
            _audio = audio;
        }

        public void Run()
        {
            if (_running)
                return;

            _running = true;
            Subscribe();
            _keyboard.Character += OnCharacter;
            _keyboard.Activate();
            _loop.Run();

            string mode = _loop.Mode == SequenceMode.Digits ? "цифры" : "буквы";
            Debug.Log("Последовательность [" + mode + "]: " + _loop.Session.Target);
        }

        private void OnCharacter(char character)
        {
            _loop.Submit(character);
        }

        private void OnUpdated()
        {
            _view.ShowSequence(_loop.Session, _loop.Mode);

            if (_initialUpdateReceived)
                _view.Pulse(_loop.Session.State == SequenceState.Lost);

            _initialUpdateReceived = true;
        }

        private void OnInputEvaluated(InputEvaluation evaluation)
        {
            AudioCue cue = evaluation switch
            {
                InputEvaluation.Correct => AudioCue.Key,
                InputEvaluation.Incorrect => AudioCue.Error,
                InputEvaluation.Completed => AudioCue.Success,
                _ => throw new System.ArgumentOutOfRangeException(nameof(evaluation), evaluation, null)
            };

            _audio.Play(cue);
        }

        private void OnResult(SequenceState state)
        {
            if (state == SequenceState.Won)
                Debug.Log("Победа — Пробел возвращает в главное меню");

            if (state == SequenceState.Lost)
                Debug.Log("Поражение — Пробел запускает новую попытку");
        }

        private void OnNavigationRequested(GameplayNavigationRequest request)
        {
            Stop();

            if (request.Destination == GameplayNavigationDestination.MainMenu)
            {
                _navigator.Go(Scenes.MainMenu);
                return;
            }

            _navigator.Go(
                Scenes.Gameplay,
                new GameplayInputArgs(request.LevelNumber, request.Mode));
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            _subscribed = true;
            _loop.InputEvaluated += OnInputEvaluated;
            _loop.Updated += OnUpdated;
            _loop.Result += OnResult;
            _loop.NavigationRequested += OnNavigationRequested;
        }

        private void Stop()
        {
            if (!_running)
                return;

            _running = false;
            _keyboard.Deactivate();
            _keyboard.Character -= OnCharacter;
            _loop.Stop();

            if (!_subscribed)
                return;

            _subscribed = false;
            _loop.InputEvaluated -= OnInputEvaluated;
            _loop.Updated -= OnUpdated;
            _loop.Result -= OnResult;
            _loop.NavigationRequested -= OnNavigationRequested;
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
