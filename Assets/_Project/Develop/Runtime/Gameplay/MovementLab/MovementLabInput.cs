using UnityEngine.InputSystem;

namespace Assets._Project.Develop.Runtime.Gameplay.MovementLab
{
    // Lab test keys; hero control goes through IInputService.
    public class MovementLabInput
    {
        public bool IsTeleportPressed() => Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;

        public bool IsDamagePressed() => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        public bool IsRandomTeleportBrainPressed() => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;

        public bool IsSmartTeleportBrainPressed() => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
    }
}
