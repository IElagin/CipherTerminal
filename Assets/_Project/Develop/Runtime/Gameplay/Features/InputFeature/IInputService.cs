using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature
{
    public interface IInputService
    {
        bool IsEnabled { get; set; }

        Vector3 Direction { get; }

        // Horizontal turn for this frame in degrees: mouse or trackpad cursor movement and the keyboard fallback.
        float RotationDelta { get; }

        bool IsAttackPressed { get; }
    }
}
