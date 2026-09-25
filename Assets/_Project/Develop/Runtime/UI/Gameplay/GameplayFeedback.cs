using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay
{
    public sealed class GameplayFeedback
    {
        private const float InputPulseOpacity = .045f;
        private const float ErrorPulseOpacity = .10f;
        private const float PulseDuration = .28f;
        private const float ShakeDuration = .25f;
        private const float ShakeFrequency = 90;
        private const float ShakeAmplitude = 5;
        private const float SweepDuration = .55f;
        private const float SweepHalfWidth = 640;
        private const float CursorBaseOpacity = .55f;
        private const float CursorOpacityAmplitude = .45f;
        private const float CursorPulseFrequency = 4;
        private const float FullOpacity = 1;

        private readonly RectTransform _rows;
        private readonly Image _pulse;
        private readonly Image _cursor;
        private readonly Image _sweep;

        private float _feedbackStartTime;
        private Color _pulseColor;
        private bool _pulseActive;
        private bool _shakeActive;
        private bool _sweepActive;
        private bool _cursorActive;

        public GameplayFeedback(RectTransform rows, Image pulse, Image cursor, Image sweep)
        {
            _rows = rows;
            _pulse = pulse;
            _cursor = cursor;
            _sweep = sweep;
        }

        public void SetInputActive(bool active, float time)
        {
            if (_cursorActive == active)
                return;

            _cursorActive = active;
            _cursor.gameObject.SetActive(active);

            if (active)
                UpdateCursorPulse(time);
        }

        public void PlayInputFeedback(float time) => StartPulse(TerminalPalette.Amber, InputPulseOpacity, time);

        public void PlayErrorFeedback(float time)
        {
            StartPulse(TerminalPalette.Error, ErrorPulseOpacity, time);
            _shakeActive = true;
        }

        public void PlayVictoryFeedback(float time)
        {
            StartPulse(TerminalPalette.Amber, InputPulseOpacity, time);
            _sweepActive = true;
            _sweep.rectTransform.anchoredPosition = new Vector2(-SweepHalfWidth, 0);
            _sweep.gameObject.SetActive(true);
        }

        public void Tick(float time)
        {
            if (_pulseActive || _shakeActive || _sweepActive)
            {
                float elapsed = time - _feedbackStartTime;

                if (_pulseActive)
                    UpdateInputPulse(elapsed);

                if (_shakeActive)
                    UpdateErrorShake(elapsed);

                if (_sweepActive)
                    UpdateVictorySweep(elapsed);
            }

            if (_cursorActive)
                UpdateCursorPulse(time);
        }

        public void Reset()
        {
            _pulseActive = false;
            _shakeActive = false;
            _sweepActive = false;
            _cursorActive = false;
            _pulse.color = Color.clear;
            _rows.anchoredPosition = Vector2.zero;
            _sweep.gameObject.SetActive(false);
            _cursor.gameObject.SetActive(false);
        }

        private void StartPulse(Color color, float opacity, float time)
        {
            _feedbackStartTime = time;
            _pulseColor = color;
            _pulseColor.a = opacity;
            _pulseActive = true;
            _pulse.color = _pulseColor;
        }

        private void UpdateInputPulse(float elapsed)
        {
            Color color = _pulseColor;
            color.a *= Mathf.Max(0, FullOpacity - elapsed / PulseDuration);
            _pulse.color = color;
            _pulseActive = elapsed < PulseDuration;
        }

        private void UpdateErrorShake(float elapsed)
        {
            if (elapsed >= ShakeDuration)
            {
                _rows.anchoredPosition = Vector2.zero;
                _shakeActive = false;
                return;
            }

            float shake = Mathf.Sin(elapsed * ShakeFrequency) * ShakeAmplitude * (FullOpacity - elapsed / ShakeDuration);
            _rows.anchoredPosition = new Vector2(shake, 0);
        }

        private void UpdateVictorySweep(float elapsed)
        {
            if (elapsed >= SweepDuration)
            {
                _sweep.gameObject.SetActive(false);
                _sweepActive = false;
                return;
            }

            float position = Mathf.Lerp(-SweepHalfWidth, SweepHalfWidth, elapsed / SweepDuration);
            _sweep.rectTransform.anchoredPosition = new Vector2(position, 0);
        }

        private void UpdateCursorPulse(float time)
        {
            Color cursor = TerminalPalette.Amber;
            cursor.a = CursorBaseOpacity + CursorOpacityAmplitude * Mathf.Sin(time * CursorPulseFrequency);
            _cursor.color = cursor;
        }
    }
}
