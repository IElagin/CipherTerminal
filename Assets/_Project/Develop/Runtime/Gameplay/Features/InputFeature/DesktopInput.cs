using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature
{
    public class DesktopInput : IInputService
    {
        private const float MouseRotationSensitivity = 0.3f;
        private const float KeyboardRotationSpeed = 180f;

        public bool IsEnabled { get; set; } = true;

        public Vector3 Direction
        {
            get
            {
                if (IsEnabled == false)
                    return Vector3.zero;

                Keyboard keyboard = Keyboard.current;

                if (keyboard == null)
                    return Vector3.zero;

                float horizontal = GetAxis(keyboard.aKey, keyboard.dKey) + GetAxis(keyboard.leftArrowKey, keyboard.rightArrowKey);
                float vertical = GetAxis(keyboard.sKey, keyboard.wKey) + GetAxis(keyboard.downArrowKey, keyboard.upArrowKey);

                return new Vector3(Mathf.Clamp(horizontal, -1, 1), 0, Mathf.Clamp(vertical, -1, 1)).normalized;
            }
        }

        public float RotationDelta
        {
            get
            {
                if (IsEnabled == false)
                    return 0;

                float rotation = 0;

                if (Mouse.current != null)
                    rotation += Mouse.current.delta.ReadValue().x * MouseRotationSensitivity;

                if (Keyboard.current != null)
                    rotation += GetAxis(Keyboard.current.qKey, Keyboard.current.eKey) * KeyboardRotationSpeed * Time.deltaTime;

                return rotation;
            }
        }

        public bool IsAttackPressed
            => IsEnabled && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        private float GetAxis(KeyControl negative, KeyControl positive)
            => (positive.isPressed ? 1 : 0) - (negative.isPressed ? 1 : 0);
    }
}
