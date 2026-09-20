namespace Assets._Project.Develop.Runtime.Gameplay.Sequence
{
    public sealed class SequenceSession
    {
        private readonly SequenceGenerator _generator;

        public SequenceSession(SequenceGenerator generator)
        {
            _generator = generator;
        }

        public string Target { get; private set; }
        public bool IsInitialized => Target != null;
        public string Entered { get; private set; } = string.Empty;
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

            char normalized = char.ToUpperInvariant(character);
            Entered += normalized;

            if (normalized != Target[Progress])
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
