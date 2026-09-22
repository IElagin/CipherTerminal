using Assets._Project.Develop.Runtime.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay
{
    public sealed class GameplayScreenView : MonoBehaviour, IView
    {
        private const float InputPulseOpacity = .045f;
        private const float ErrorPulseOpacity = .10f;
        private const float PulseDuration = .28f;
        private const float ShakeDuration = .25f;
        private const float ShakeFrequency = 90;
        private const float ShakeAmplitude = 5;
        private const float SweepDuration = .55f;
        private const float SweepHalfWidth = 640;
        private const float CursorVerticalOffset = -48;
        private const float CursorBaseOpacity = .55f;
        private const float CursorOpacityAmplitude = .45f;
        private const float CursorPulseFrequency = 4;
        private const float FullOpacity = 1;
        private const int NoCellIndex = -1;

        [SerializeField] private TMP_Text _protocol;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private TMP_Text _outcome;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private TMP_Text _saveError;
        [SerializeField] private SequenceRowView _targetRow;
        [SerializeField] private SequenceRowView _enteredRow;
        [SerializeField] private RectTransform _rows;
        [SerializeField] private Image _pulse;
        [SerializeField] private Image _cursor;
        [SerializeField] private Image _sweep;

        private float _feedbackStartTime;
        private Color _pulseColor;
        private bool _pulseActive;
        private bool _shakeActive;
        private bool _sweepActive;
        private bool _cursorActive;

        public void Render(string protocol, string target, string entered, int verified, bool acceptingInput, bool won)
        {
            _protocol.text = protocol;
            _progress.text = verified + " / " + target.Length + "  ПРОВЕРЕНО";
            int errorIndex = acceptingInput || won ? NoCellIndex : verified;
            int cursorIndex = acceptingInput ? verified : NoCellIndex;
            _targetRow.Render(target, target.Length, verified, errorIndex, cursorIndex);
            _enteredRow.Render(entered, target.Length, verified, errorIndex, cursorIndex);
            _outcome.text = acceptingInput ? "" : won ? "ДОСТУП РАЗРЕШЁН" : "ДОСТУП ЗАПРЕЩЁН";
            _outcome.color = won ? TerminalPalette.Amber : TerminalPalette.Error;
            _hint.text = acceptingInput ? "ВВЕДИТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ" : won
                ? "ПРОБЕЛ / ВЕРНУТЬСЯ В МЕНЮ" : "ПРОБЕЛ / ПОВТОРИТЬ ПРОТОКОЛ";

            if (acceptingInput)
                _cursor.rectTransform.anchoredPosition = new Vector2(_enteredRow.GetPosition(verified, target.Length), CursorVerticalOffset);
        }

        public void ShowSaveError(string message) => _saveError.text = message ?? "";

        public void SetInputActive(bool active)
        {
            if (_cursorActive == active)
                return;

            _cursorActive = active;
            _cursor.gameObject.SetActive(active);

            if (active)
                UpdateCursorPulse();
        }

        public void PlayInputFeedback() => StartPulse(TerminalPalette.Amber, InputPulseOpacity);

        public void PlayErrorFeedback()
        {
            StartPulse(TerminalPalette.Error, ErrorPulseOpacity);
            _shakeActive = true;
        }

        public void PlayVictoryFeedback()
        {
            StartPulse(TerminalPalette.Amber, InputPulseOpacity);
            _sweepActive = true;
            _sweep.rectTransform.anchoredPosition = new Vector2(-SweepHalfWidth, 0);
            _sweep.gameObject.SetActive(true);
        }

        private void Awake() => ResetEffects();

        private void OnDisable() => ResetEffects();

        private void Update()
        {
            if (_pulseActive || _shakeActive || _sweepActive)
            {
                float elapsed = Time.unscaledTime - _feedbackStartTime;

                if (_pulseActive)
                    UpdateInputPulse(elapsed);

                if (_shakeActive)
                    UpdateErrorShake(elapsed);

                if (_sweepActive)
                    UpdateVictorySweep(elapsed);
            }

            if (_cursorActive)
                UpdateCursorPulse();
        }

        private void StartPulse(Color color, float opacity)
        {
            _feedbackStartTime = Time.unscaledTime;
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

        private void UpdateCursorPulse()
        {
            Color cursor = TerminalPalette.Amber;
            cursor.a = CursorBaseOpacity + CursorOpacityAmplitude * Mathf.Sin(Time.unscaledTime * CursorPulseFrequency);
            _cursor.color = cursor;
        }

        private void ResetEffects()
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
    }
}
