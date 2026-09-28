using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets._Project.Develop.Runtime.Gameplay.MovementLab
{
    public class MovementLabInput
    {
        public Vector3 ReadDirection()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return Vector3.zero;

            float horizontal = 0;
            float vertical = 0;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontal++;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontal--;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                vertical++;

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                vertical--;

            return new Vector3(horizontal, 0, vertical).normalized;
        }
    }
}
