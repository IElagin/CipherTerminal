using System;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay
{
    public sealed class GameplayLoop
    {
        private readonly GameplayInputArgs _args;

        private bool _running;
        private bool _stopped;
        private bool _handlingInput;
        private bool _navigationRequested;

        public SequenceSession Session { get; }
        public SequenceMode Mode => _args.Mode;

        public event Action Updated;
        public event Action<InputEvaluation> InputEvaluated;
        public event Action<SequenceState> Result;
        public event Action<GameplayNavigationRequest> NavigationRequested;

        public GameplayLoop(SequenceSession session, GameplayInputArgs args)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _args = args ?? throw new ArgumentNullException(nameof(args));
        }

        public void Run()
        {
            if (_running || _stopped)
                return;

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
            if (!_running || _stopped || _handlingInput || _navigationRequested)
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
            if (!Session.Submit(character))
                return;

            InputEvaluation evaluation = Session.State switch
            {
                SequenceState.Won => InputEvaluation.Completed,
                SequenceState.Lost => InputEvaluation.Incorrect,
                _ => InputEvaluation.Correct
            };

            InputEvaluated?.Invoke(evaluation);

            if (!_running || _stopped)
                return;

            Updated?.Invoke();

            if (_running && !_stopped && Session.State != SequenceState.Input)
                Result?.Invoke(Session.State);
        }

        private void RequestNavigation()
        {
            _navigationRequested = true;

            GameplayNavigationDestination destination = Session.State == SequenceState.Won
                ? GameplayNavigationDestination.MainMenu
                : GameplayNavigationDestination.Retry;

            NavigationRequested?.Invoke(new GameplayNavigationRequest(
                destination,
                _args.LevelNumber,
                _args.Mode));
        }
    }
}
