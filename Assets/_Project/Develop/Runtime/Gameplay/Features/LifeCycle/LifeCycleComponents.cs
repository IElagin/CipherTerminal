using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.LifeCycle
{
    public class CurrentHealth : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class MaxHealth : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class IsDead : IEntityComponent
    {
        public ReactiveVariable<bool> Value;
    }

    public class MustDie : IEntityComponent
    {
        public ICompositeCondition Value;
    }

    public class MustSelfRelease : IEntityComponent
    {
        public ICompositeCondition Value;
    }

    public class DeathProcessInitialTime : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class DeathProcessCurrentTime : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class InDeathProcess : IEntityComponent
    {
        public ReactiveVariable<bool> Value;
    }

    public class DisableCollidersOnDeath : IEntityComponent
    {
        public List<Collider> Value;
    }
}
