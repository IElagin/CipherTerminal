using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Common
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerEntityRegistrator : MonoEntityRegistrator
    {
        public override void Register(Entity entity) => entity.AddCharacterController(GetComponent<CharacterController>());
    }
}
