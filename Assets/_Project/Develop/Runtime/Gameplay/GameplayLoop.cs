using System;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay
{
    public sealed class GameplayLoop
    {
        private readonly GameplayInputArgs _gameplayInputArgs;

        private bool _running;
        private bool _stopped;
        private bool _handlingInput;
        private bool _navigationRequested;

        public event Action Updated;
        public event Action<InputEvaluation> InputEvaluated;
        public event Action<SequenceState> Finished;
        public event Action<GameplayNavigationRequest> NavigationRequested;

        public GameplayLoop(SequenceSession session, GameplayInputArgs gameplayInputArgs)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _gameplayInputArgs = gameplayInputArgs ?? throw new ArgumentNullException(nameof(gameplayInputArgs));
        }

        public SequenceSession Session { get; }
        public SequenceMode Mode => _gameplayInputArgs.Mode;

        public void Run()
        {
            if (_running || _stopped)
                return;

            if (Session.IsInitialized == false)
                throw new InvalidOperationException("Initialize the sequence session before running gameplay");

            _running = true;
            _handlingInput = true;

            try
            {
                Updated?.Invoke();
            }
            finally
            {
                _handlingInput = false;
            }
        }

        public void Submit(char character)
        {
            if (_running == false || _stopped || _handlingInput || _navigationRequested)
                return;

            _handlingInput = true;

            try
            {
                if (Session.State == SequenceState.Input)
                {
                    SubmitSequenceCharacter(character);
                    return;
                }

                if (character == ' ')
                    RequestNavigation();
            }
            finally
            {
                _handlingInput = false;
            }
        }

        public void Stop()
        {
            _stopped = true;
            _running = false;
        }

        public void AllowNavigationRetry()
        {
            if (_stopped || Session.State == SequenceState.Input)
                return;

            _navigationRequested = false;
        }

        private void SubmitSequenceCharacter(char character)
        {
            if (Session.Submit(character) == false)
                return;

            InputEvaluation evaluation = Session.State switch
            {
                SequenceState.Won => InputEvaluation.Completed,
                SequenceState.Lost => InputEvaluation.Incorrect,
                _ => InputEvaluation.Correct
            };

            InputEvaluated?.Invoke(evaluation);

            if (_running == false || _stopped)
                return;

            Updated?.Invoke();

            if (_running && _stopped == false && Session.State != SequenceState.Input)
                Finished?.Invoke(Session.State);
        }

        private void RequestNavigation()
        {
            _navigationRequested = true;

            GameplayNavigationDestination destination = Session.State == SequenceState.Won
                ? GameplayNavigationDestination.MainMenu
                : GameplayNavigationDestination.Retry;

            NavigationRequested?.Invoke(new GameplayNavigationRequest(
                destination,
                _gameplayInputArgs.LevelNumber,
                _gameplayInputArgs.Mode));
        }
    }
}
