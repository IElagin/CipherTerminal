using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI
{
    public class CurrentTarget : IEntityComponent
    {
        public ReactiveVariable<Entity> Value;
    }
}
