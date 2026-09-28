namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems
{
    public interface IFixedUpdatableSystem : IEntitySystem
    {
        public void OnFixedUpdate(float deltaTime);
    }
}
