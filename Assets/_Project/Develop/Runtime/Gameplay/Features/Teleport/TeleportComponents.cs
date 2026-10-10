using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Teleport
{
    public class TeleportCenter : IEntityComponent
    {
        public ReactiveVariable<Vector3> Value;
    }

    public class TeleportRadius : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class TeleportEnergyCost : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class TeleportRequest : IEntityComponent
    {
        public ReactiveEvent Value;
    }

    public class TeleportToPointRequest : IEntityComponent
    {
        public ReactiveEvent<Vector3> Value;
    }

    public class TeleportEvent : IEntityComponent
    {
        public ReactiveEvent<Vector3> Value;
    }

    public class CanTeleport : IEntityComponent
    {
        public ICompositeCondition Value;
    }
}
