using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Common
{
    public class RigidbodyComponent : IEntityComponent
    {
        public Rigidbody Value;
    }

    public class CharacterControllerComponent : IEntityComponent
    {
        public CharacterController Value;
    }

    public class TransformComponent : IEntityComponent
    {
        public Transform Value;
    }

    public class AnimatorComponent : IEntityComponent
    {
        public Animator Value;
    }
}
