using System;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public readonly struct EconomyRules
    {
        public EconomyRules(int initialGold, int winReward, int lossPenalty, int statisticsResetCost)
        {
            if (initialGold < 0)
                throw new ArgumentOutOfRangeException(nameof(initialGold));

            if (winReward < 0)
                throw new ArgumentOutOfRangeException(nameof(winReward));

            if (lossPenalty < 0)
                throw new ArgumentOutOfRangeException(nameof(lossPenalty));

            if (statisticsResetCost < 0)
                throw new ArgumentOutOfRangeException(nameof(statisticsResetCost));

            InitialGold = initialGold;
            WinReward = winReward;
            LossPenalty = lossPenalty;
            StatisticsResetCost = statisticsResetCost;
        }

        public int InitialGold { get; }
        public int WinReward { get; }
        public int LossPenalty { get; }
        public int StatisticsResetCost { get; }
    }
}
