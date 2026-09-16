using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository
{
    public interface IDataRepository
    {
        public UniTask<string> ReadAsync(string key, CancellationToken cancellationToken = default);

        public UniTask WriteAsync(string key, string serializedData, CancellationToken cancellationToken = default);

        public UniTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}
