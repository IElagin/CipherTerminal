using System;
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
            string key = _keysStorage.GetKeyFor<TData>();

            try
            {
                string serialized = await _repository.ReadAsync(key, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                return _serializer.Deserialize<TData>(serialized);
            }
            catch (Exception exception) when (SaveDataException.IsExpectedFailure(exception))
            {
                throw new SaveDataException(exception);
            }
        }

        public async UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default)
            where TData : class, ISaveData
        {
            cancellationToken.ThrowIfCancellationRequested();
            string key = _keysStorage.GetKeyFor<TData>();

            try
            {
                string serialized = _serializer.Serialize(data);
                await _repository.WriteAsync(key, serialized, cancellationToken);
            }
            catch (Exception exception) when (SaveDataException.IsExpectedFailure(exception))
            {
                throw new SaveDataException(exception);
            }
        }

        public async UniTask<bool> ExistsAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData
        {
            string key = _keysStorage.GetKeyFor<TData>();

            try
            {
                return await _repository.ExistsAsync(key, cancellationToken);
            }
            catch (Exception exception) when (SaveDataException.IsExpectedFailure(exception))
            {
                throw new SaveDataException(exception);
            }
        }
    }
}
