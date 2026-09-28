namespace Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems
{
    public interface IInitializableSystem : IEntitySystem
    {
        public void OnInit(Entity entity);
    }
}
