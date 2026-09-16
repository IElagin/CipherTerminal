using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities.ConfigsManagement
{
    public interface IConfigsLoader
    {
        public UniTask<Dictionary<Type, object>> LoadAsync(CancellationToken cancellationToken = default);
    }
}
