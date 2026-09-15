using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public sealed class StatisticsService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
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

        internal void SetCounts(int wins, int losses)
        {
            Wins = wins;
            Losses = losses;
        }
    }
}
