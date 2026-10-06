using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Energy
{
    public class MaxEnergy : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class CurrentEnergy : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class EnergyRegenerationPercent : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class EnergyRegenerationInitialTime : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class EnergyRegenerationCurrentTime : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }
}
