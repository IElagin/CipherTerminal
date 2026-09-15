using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment
{
    public interface ISaveLoadService
    {
        UniTask<TData> LoadAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData;

        UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default)
            where TData : class, ISaveData;

        UniTask<bool> ExistsAsync<TData>(CancellationToken cancellationToken = default)
            where TData : class, ISaveData;
    }
}
