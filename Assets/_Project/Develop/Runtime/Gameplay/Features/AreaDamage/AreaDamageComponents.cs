using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class AreaDamage : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class AreaDamageRadius : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class AreaDamageMask : IEntityComponent
    {
        public LayerMask Value;
    }

    public class AreaDamageCollidersBuffer : IEntityComponent
    {
        public Buffer<Collider> Value;
    }

    public class AreaDamageCollidersDetectedEvent : IEntityComponent
    {
        public ReactiveEvent Value;
    }

    public class AreaDamageEntitiesBuffer : IEntityComponent
    {
        public Buffer<Entity> Value;
    }

    public class AreaDamageTargetsDetectedEvent : IEntityComponent
    {
        public ReactiveEvent Value;
    }

    public class AreaDamageZoneView : IEntityComponent
    {
        public GameObject Value;
    }

    public class AreaDamageZoneShowTime : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class AreaDamageZoneCurrentTime : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }
}
