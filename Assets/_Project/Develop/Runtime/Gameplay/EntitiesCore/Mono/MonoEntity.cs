using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono
{
    public class MonoEntity : MonoBehaviour
    {
        private MonoEntityRegistrator[] _registrators;

        public void Setup(Entity entity)
        {
            _registrators = GetComponentsInChildren<MonoEntityRegistrator>(true);

            foreach (MonoEntityRegistrator registrator in _registrators)
                registrator.Register(entity);
        }

        public void Cleanup(Entity entity)
        {
            foreach (MonoEntityRegistrator registrator in _registrators)
                registrator.Unregister(entity);
        }
    }
}
