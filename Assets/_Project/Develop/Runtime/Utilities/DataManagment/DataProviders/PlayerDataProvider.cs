using Assets._Project.Develop.Runtime.Meta.Progress;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders
{
    public sealed class PlayerDataProvider : DataProvider<PlayerData>
    {
        private readonly EconomyRules _rules;

        public PlayerDataProvider(ISaveLoadService saveLoadService, EconomyRules rules)
            : base(saveLoadService)
        {
            _rules = rules;
        }

        protected override PlayerData CreateOriginData()
            => new PlayerData(_rules.InitialGold, 0, 0);

        protected override void Validate(PlayerData data)
        {
            data.Validate();
        }
    }
}
