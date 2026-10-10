using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.LifeCycle;
using Assets._Project.Develop.Runtime.Utilities.Conditions;
using System.Collections.Generic;
using System.Linq;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AI.States
{
    public class LowestHealthTargetSelector : ITargetSelector
    {
        private Entity _source;

        public LowestHealthTargetSelector(Entity entity)
        {
            _source = entity;
        }

        public Entity SelectTargetFrom(IEnumerable<Entity> targets)
        {
            IEnumerable<Entity> selectedTargets = targets.Where(target =>
            {
                bool result = target.HasComponent<TakeDamageRequest>() && target.HasComponent<CurrentHealth>();

                if (target.TryGetCanApplyDamage(out ICompositeCondition canApplyDamage))
                {
                    result = result && canApplyDamage.Evaluate();
                }

                result = result && (target != _source);

                return result;
            });

            if (selectedTargets.Any() == false)
                return null;

            Entity weakestTarget = selectedTargets.First();
            float minHealth = weakestTarget.CurrentHealth.Value;

            foreach (Entity target in selectedTargets)
            {
                float health = target.CurrentHealth.Value;

                if (health < minHealth)
                {
                    minHealth = health;
                    weakestTarget = target;
                }
            }

            return weakestTarget;
        }
    }
}
