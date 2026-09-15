using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Infrastructure
{
    public abstract class SceneBootstrap : MonoBehaviour
    {
        public abstract void ProcessRegistrations(IContainerBuilder builder, IInputSceneArgs sceneArgs = null);
        public abstract UniTask InitializeAsync(IObjectResolver container, CancellationToken cancellationToken);
        public abstract void Run();
    }
}
