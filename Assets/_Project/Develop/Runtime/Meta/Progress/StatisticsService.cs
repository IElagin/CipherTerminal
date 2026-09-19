using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public sealed class StatisticsService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        private const int ResultCountIncrement = 1;

        public int Wins { get; private set; }
        public int Losses { get; private set; }

        public void ReadFrom(PlayerData data)
        {
            Wins = data.Wins;
            Losses = data.Losses;
        }

        public void WriteTo(PlayerData data)
        {
            data.Wins = Wins;
            data.Losses = Losses;
        }

        public void RecordWin() => Wins = checked(Wins + ResultCountIncrement);

        public void RecordLoss() => Losses = checked(Losses + ResultCountIncrement);

        public void Reset()
        {
            Wins = 0;
            Losses = 0;
        }
    }
}
