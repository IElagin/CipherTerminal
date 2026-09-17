using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders
{
    public abstract class DataProvider<TData> : IDisposable where TData : class, ISaveData
    {
        private readonly ISaveLoadService _saveLoadService;
        private readonly List<IDataReader<TData>> _readers = new List<IDataReader<TData>>();
        private readonly List<IDataWriter<TData>> _writers = new List<IDataWriter<TData>>();

        private TData _data;
        private bool _disposed;

        protected DataProvider(ISaveLoadService saveLoadService)
        {
            _saveLoadService = saveLoadService;
        }

        public void RegisterReader(IDataReader<TData> reader)
        {
            EnsureNotDisposed();

            if (_readers.Contains(reader))
                throw new ArgumentException("Reader already registered", nameof(reader));

            _readers.Add(reader);
        }

        public void RegisterWriter(IDataWriter<TData> writer)
        {
            EnsureNotDisposed();

            if (_writers.Contains(writer))
                throw new ArgumentException("Writer already registered", nameof(writer));

            _writers.Add(writer);
        }

        public void UnregisterReader(IDataReader<TData> reader)
        {
            _readers.Remove(reader);
        }

        public void UnregisterWriter(IDataWriter<TData> writer)
        {
            _writers.Remove(writer);
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            TData loaded = await _saveLoadService.LoadAsync<TData>(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotDisposed();

            if (loaded == null)
                throw new SaveDataException(new InvalidDataException("Save contains null data"));

            try
            {
                Validate(loaded);
            }
            catch (Exception exception) when (SaveDataException.IsExpectedFailure(exception))
            {
                throw new SaveDataException(exception);
            }

            _data = loaded;
            SendToReaders();
        }

        public async UniTask SaveAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (_data == null)
                throw new InvalidOperationException("Load or reset data before saving");

            foreach (IDataWriter<TData> writer in _writers.ToArray())
                writer.WriteTo(_data);

            Validate(_data);
            await _saveLoadService.SaveAsync(_data, cancellationToken);
        }

        public UniTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _saveLoadService.ExistsAsync<TData>(cancellationToken);
        }

        public void Reset()
        {
            EnsureNotDisposed();
            TData origin = CreateOriginData();
            Validate(origin);
            _data = origin;
            SendToReaders();
        }

        public void Dispose()
        {
            _disposed = true;
            _readers.Clear();
            _writers.Clear();
            _data = null;
        }

        protected abstract TData CreateOriginData();

        protected abstract void Validate(TData data);

        private void SendToReaders()
        {
            foreach (IDataReader<TData> reader in _readers.ToArray())
                reader.ReadFrom(_data);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
