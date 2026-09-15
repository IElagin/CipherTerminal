using System.Threading;
using Cysharp.Threading.Tasks;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment
{
    public sealed class SaveLoadService : ISaveLoadService
    {
        private readonly IDataSerializer _serializer;
        private readonly IDataKeysStorage _keysStorage;
        private readonly IDataRepository _repository;

        public SaveLoadService(IDataSerializer serializer, IDataKeysStorage keysStorage, IDataRepository repository)
        {
            _serializer = serializer;
            _keysStorage = keysStorage;
            _repository = repository;
        }

        public async UniTask<TData> LoadAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData
        {
            string serialized = await _repository.ReadAsync(_keysStorage.GetKeyFor<TData>(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return _serializer.Deserialize<TData>(serialized);
        }

        public UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default)
            where TData : class, ISaveData
        {
            cancellationToken.ThrowIfCancellationRequested();
            string serialized = _serializer.Serialize(data);
            return _repository.WriteAsync(_keysStorage.GetKeyFor<TData>(), serialized, cancellationToken);
        }

        public UniTask<bool> ExistsAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData
            => _repository.ExistsAsync(_keysStorage.GetKeyFor<TData>(), cancellationToken);
    }
}
