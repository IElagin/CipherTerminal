namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems
{
    public interface IUpdatableSystem : IEntitySystem
    {
        public void OnUpdate(float deltaTime);
    }
}
