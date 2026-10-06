using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Mono;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AreaDamage
{
    public class AreaDamageZoneRegistrator : MonoEntityRegistrator
    {
        [SerializeField] private GameObject _zone;

        public override void Register(Entity entity) => entity.AddAreaDamageZoneView(_zone);
    }
}
