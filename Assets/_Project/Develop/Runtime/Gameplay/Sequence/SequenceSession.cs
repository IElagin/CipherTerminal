namespace Assets._Project.Develop.Runtime.Gameplay.Sequence
{
    public sealed class SequenceSession
    {
        private readonly SequenceGenerator _generator;

        public SequenceSession(SequenceGenerator generator)
        {
            _generator = generator ?? throw new System.ArgumentNullException(nameof(generator));
        }

        public string Target { get; private set; }
        public bool IsInitialized => Target != null;
        public int Progress { get; private set; }
        public SequenceState State { get; private set; }

        public void Initialize(string symbols, int length)
        {
            if (IsInitialized)
                throw new System.InvalidOperationException("Sequence session is already initialized");

            Target = _generator.Generate(symbols, length).ToUpperInvariant();
        }

        public bool Submit(char character)
        {
            if (IsInitialized == false)
                throw new System.InvalidOperationException("Initialize the sequence session before submitting input");

            if (State != SequenceState.Input || char.IsControl(character))
                return false;

            if (char.ToUpperInvariant(character) != Target[Progress])
            {
                State = SequenceState.Lost;
                return true;
            }

            Progress++;

            if (Progress == Target.Length)
                State = SequenceState.Won;

            return true;
        }
    }
}
