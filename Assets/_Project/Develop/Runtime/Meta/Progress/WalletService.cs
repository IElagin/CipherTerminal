using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public sealed class WalletService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        public int Gold { get; private set; }

        public void ReadFrom(PlayerData data)
        {
            Gold = data.Gold;
        }

        public void WriteTo(PlayerData data)
        {
            data.Gold = Gold;
        }

        internal void SetGold(int gold)
        {
            Gold = gold;
        }
    }
}
