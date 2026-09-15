using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository
{
    public interface IDataRepository
    {
        UniTask<string> ReadAsync(string key, CancellationToken cancellationToken = default);
        UniTask WriteAsync(string key, string serializedData, CancellationToken cancellationToken = default);
        UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}
