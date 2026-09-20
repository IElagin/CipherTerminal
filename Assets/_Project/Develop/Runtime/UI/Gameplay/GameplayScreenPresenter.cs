using Assets._Project.Develop.Runtime.Gameplay;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;
using Assets._Project.Develop.Runtime.Meta.Progress;
using Assets._Project.Develop.Runtime.UI.Core;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using Assets._Project.Develop.Runtime.Utilities.Input;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.UI.Gameplay
{
    public sealed class GameplayScreenPresenter : IPresenter
    {
        private readonly GameplayScreenView _view;
        private readonly TerminalKeyboard _keyboard;
        private readonly GameplayLoop _loop;
        private readonly SceneNavigator _navigator;
        private readonly IAudioService _audio;
        private readonly PlayerProgressService _progress;
        private bool _initialUpdateReceived;

        public GameplayScreenPresenter(GameplayScreenView view, TerminalKeyboard keyboard, GameplayLoop loop,
            SceneNavigator navigator, IAudioService audio, PlayerProgressService progress)
        {
            _view = view;
            _keyboard = keyboard;
            _loop = loop;
            _navigator = navigator;
            _audio = audio;
            _progress = progress;
        }

        public void Initialize()
        {
            _loop.Updated += OnUpdated;
            _loop.InputEvaluated += OnInputEvaluated;
            _loop.NavigationRequested += OnNavigationRequested;
            _progress.Changed += OnProgressChanged;
            _view.ShowSaveError(_progress.Error);
        }

        public void Run()
        {
            _keyboard.CharacterEntered += OnCharacterEntered;
            _keyboard.Activate();
            _loop.Run();
        }

        public void Dispose()
        {
            _keyboard.Deactivate();
            _keyboard.CharacterEntered -= OnCharacterEntered;
            _loop.Stop();
            _loop.Updated -= OnUpdated;
            _loop.InputEvaluated -= OnInputEvaluated;
            _loop.NavigationRequested -= OnNavigationRequested;
            _progress.Changed -= OnProgressChanged;
        }

        private void OnCharacterEntered(char character) => _loop.Submit(character);

        private void OnProgressChanged(ProgressSnapshot snapshot) => _view.ShowSaveError(_progress.Error);

        private void OnUpdated()
        {
            SequenceSession session = _loop.Session;
            string protocol = _loop.Mode == SequenceMode.Digits ? "ЦИФРОВОЙ ПРОТОКОЛ" : "БУКВЕННЫЙ ПРОТОКОЛ";
            _view.Render(protocol, session.Target, session.Entered, session.Progress,
                session.State == SequenceState.Input, session.State == SequenceState.Won);

            if (_initialUpdateReceived)
                _view.Pulse(session.State == SequenceState.Lost);

            _initialUpdateReceived = true;
        }

        private void OnInputEvaluated(InputEvaluation evaluation)
        {
            AudioCue cue = evaluation switch
            {
                InputEvaluation.Correct => AudioCue.Key,
                InputEvaluation.Incorrect => AudioCue.Error,
                InputEvaluation.Completed => AudioCue.Success,
                _ => throw new System.ArgumentOutOfRangeException(nameof(evaluation))
            };
            _audio.Play(cue);
        }

        private void OnNavigationRequested(GameplayNavigationRequest request)
        {
            _keyboard.Deactivate();

            if (request.Destination == GameplayNavigationDestination.MainMenu)
                _navigator.Go(Scenes.MainMenu);
            else
                _navigator.Go(Scenes.Gameplay, new GameplayInputArgs(request.LevelNumber, request.Mode));
        }
    }
}
