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
        private bool _navigationRequested;

        public event Action Updated;
        public event Action<InputEvaluation> InputEvaluated;
        public event Action<SequenceState> Finished;
        public event Action<GameplayNavigationRequest> NavigationRequested;

        public GameplayLoop(SequenceSession session, GameplayInputArgs gameplayInputArgs)
        {
            Session = session;
            _gameplayInputArgs = gameplayInputArgs;
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

            Updated?.Invoke();
        }

        public void Submit(char character)
        {
            if (_running == false || _stopped || _navigationRequested)
                return;

            if (Session.State == SequenceState.Input)
                SubmitSequenceCharacter(character);
            else if (character == ' ')
                RequestNavigation();
        }

        public void Stop()
        {
            _stopped = true;
            _running = false;
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
            Updated?.Invoke();

            if (Session.State != SequenceState.Input)
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
