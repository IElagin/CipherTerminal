using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets._Project.Develop.Runtime.Utilities.Input
{
    public sealed class TerminalKeyboard : MonoBehaviour
    {
        private readonly Queue<char> _characters = new Queue<char>();

        private Keyboard _keyboard;
        private bool _active;
        private bool _armed;
        private int _startFrame;

        public event Action<char> CharacterEntered;
        public event Action EscapePressed;

        public void Activate()
        {
            _active = true;
            _armed = false;
            _startFrame = Time.frameCount;
        }

        public void Deactivate()
        {
            _active = false;
            _characters.Clear();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void Bind()
        {
            if (_keyboard != null)
                _keyboard.onTextInput -= OnText;

            _keyboard = Keyboard.current;

            if (_keyboard != null)
                _keyboard.onTextInput += OnText;

            _armed = false;
            _characters.Clear();
        }

        private void OnDisable()
        {
            if (_keyboard != null)
                _keyboard.onTextInput -= OnText;

            _keyboard = null;
            _characters.Clear();
        }

        private void OnText(char character)
        {
            if (_active && _armed && char.IsControl(character) == false)
                _characters.Enqueue(character);
        }

        private void Update()
        {
            if (_keyboard != Keyboard.current)
                Bind();

            if (_active == false || _keyboard == null)
            {
                _characters.Clear();
                return;
            }

            if (_armed == false)
            {
                _characters.Clear();

                if (Time.frameCount > _startFrame && HasCharacterKey(false) == false)
                    _armed = true;

                return;
            }

            if (_keyboard.escapeKey.wasPressedThisFrame)
            {
                _characters.Clear();
                EscapePressed?.Invoke();
                return;
            }

            bool spacePressed = _keyboard.spaceKey.wasPressedThisFrame;

            // The physical edge is the fallback when the platform sends no text event.
            // Matching queued spaces are discarded below so one press has one delivery.

            if (spacePressed)
                CharacterEntered?.Invoke(' ');

            if (HasCharacterKey(true) == false)
            {
                _characters.Clear();
                return;
            }

            while (_characters.Count > 0 && _active)
            {
                char character = _characters.Dequeue();

                if (character == ' ')
                    continue;

                CharacterEntered?.Invoke(character);
            }
        }

        private bool HasCharacterKey(bool pressedThisFrame)
        {
            foreach (var key in _keyboard.allKeys)
            {
                if (key.keyCode == Key.LeftShift || key.keyCode == Key.RightShift ||
                    key.keyCode == Key.LeftCtrl || key.keyCode == Key.RightCtrl ||
                    key.keyCode == Key.LeftAlt || key.keyCode == Key.RightAlt ||
                    key.keyCode == Key.LeftMeta || key.keyCode == Key.RightMeta)
                {
                    continue;
                }

                if (pressedThisFrame ? key.wasPressedThisFrame : key.isPressed)
                    return true;
            }

            return false;
        }
    }
}
