using System;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public readonly struct ProgressSnapshot : IEquatable<ProgressSnapshot>
    {
        public int Gold { get; }
        public int Wins { get; }
        public int Losses { get; }

        public ProgressSnapshot(int gold, int wins, int losses)
        {
            Gold = gold;
            Wins = wins;
            Losses = losses;
        }

        public bool Equals(ProgressSnapshot other)
            => Gold == other.Gold && Wins == other.Wins && Losses == other.Losses;

        public override bool Equals(object obj)
            => obj is ProgressSnapshot other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Gold, Wins, Losses);
    }
}
