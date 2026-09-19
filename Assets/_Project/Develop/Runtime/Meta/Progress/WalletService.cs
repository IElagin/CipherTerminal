using System;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Utilities.DataManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public sealed class WalletService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        private readonly Dictionary<CurrencyTypes, ReactiveVariable<int>> _currencies;

        public WalletService(Dictionary<CurrencyTypes, ReactiveVariable<int>> currencies)
        {
            _currencies = new Dictionary<CurrencyTypes, ReactiveVariable<int>>(currencies);
        }

        public List<CurrencyTypes> AvailableCurrencies => _currencies.Keys.ToList();

        public void ReadFrom(PlayerData data)
        {
            _currencies[CurrencyTypes.Gold].Value = data.Gold;
        }

        public void WriteTo(PlayerData data)
        {
            data.Gold = _currencies[CurrencyTypes.Gold].Value;
        }

        public IReadOnlyVariable<int> GetCurrency(CurrencyTypes currencyType) => _currencies[currencyType];

        public bool HasEnough(CurrencyTypes currencyType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            return _currencies[currencyType].Value >= amount;
        }

        public void Add(CurrencyTypes currencyType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            _currencies[currencyType].Value += amount;
        }

        public void Spend(CurrencyTypes currencyType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (HasEnough(currencyType, amount) == false)
                throw new InvalidOperationException("Not enough: " + currencyType);

            _currencies[currencyType].Value -= amount;
        }
    }
}
