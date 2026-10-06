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
}
