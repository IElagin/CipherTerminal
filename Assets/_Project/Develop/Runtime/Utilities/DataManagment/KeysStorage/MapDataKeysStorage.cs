using System;
using System.Collections.Generic;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage
{
    public sealed class MapDataKeysStorage : IDataKeysStorage
    {
        private readonly Dictionary<Type, string> _keys = new Dictionary<Type, string>
        {
            { typeof(PlayerData), "player" }
        };

        public string GetKeyFor<TData>() where TData : class, ISaveData
        {
            if (!_keys.TryGetValue(typeof(TData), out string key))
                throw new InvalidOperationException("No save key for " + typeof(TData));

            return key;
        }
    }
}
