using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment
{
    public interface ISaveLoadService
    {
        public UniTask<TData> LoadAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData;

        public UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default)
            where TData : class, ISaveData;

        public UniTask<bool> ExistsAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData;
    }
}
