namespace Assets._Project.Develop.Runtime.Gameplay.Sequence
{
    public sealed class SequenceGenerator
    {
        public const int MinimumLength = 1;
        public const int MaximumLength = 12;

        private readonly System.Random _random;

        public SequenceGenerator(System.Random random)
        {
            _random = random;
        }

        public string Generate(string symbols, int length)
        {
            if (string.IsNullOrEmpty(symbols) || length < MinimumLength || length > MaximumLength)
                throw new System.ArgumentException("Invalid sequence configuration");

            char[] result = new char[length];

            for (int i = 0; i < length; i++)
                result[i] = symbols[_random.Next(symbols.Length)];

            return new string(result);
        }
    }
}
