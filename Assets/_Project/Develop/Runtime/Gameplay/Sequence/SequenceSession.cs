namespace Assets._Project.Develop.Runtime.Gameplay.Sequence
{
    public sealed class SequenceSession
    {
        public string Target { get; }
        public int Progress { get; private set; }
        public SequenceState State { get; private set; }

        public SequenceSession(string target)
        {
            if (string.IsNullOrEmpty(target))
                throw new System.ArgumentException("Sequence must not be empty", nameof(target));

            Target = target.ToUpperInvariant();
        }

        public bool Submit(char character)
        {
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
