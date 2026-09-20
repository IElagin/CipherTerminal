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

        private float _pulseStart = -10;
        private bool _error;
        private bool _won;

        public void Render(string protocol, string target, string entered, int verified, bool acceptingInput, bool won)
        {
            _won = won;
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
            _cursor.gameObject.SetActive(acceptingInput);

            if (acceptingInput)
                _cursor.rectTransform.anchoredPosition = new Vector2(_enteredRow.GetPosition(verified, target.Length), CursorVerticalOffset);
        }

        public void ShowSaveError(string message) => _saveError.text = message ?? "";

        public void Pulse(bool error)
        {
            _pulseStart = Time.unscaledTime;
            _error = error;
        }

        private void Update()
        {
            float age = Time.unscaledTime - _pulseStart;
            Color color = _error ? TerminalPalette.Error : TerminalPalette.Amber;
            color.a = Mathf.Max(0, FullOpacity - age / PulseDuration) * (_error ? ErrorPulseOpacity : InputPulseOpacity);
            _pulse.color = color;
            float shake = _error && age < ShakeDuration ? Mathf.Sin(age * ShakeFrequency) * ShakeAmplitude * (FullOpacity - age / ShakeDuration) : 0;
            _rows.anchoredPosition = new Vector2(shake, 0);
            bool sweeping = _won && age >= 0 && age < SweepDuration;
            _sweep.gameObject.SetActive(sweeping);

            if (sweeping)
                _sweep.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-SweepHalfWidth, SweepHalfWidth, age / SweepDuration), 0);

            Color cursor = TerminalPalette.Amber;
            cursor.a = CursorBaseOpacity + CursorOpacityAmplitude * Mathf.Sin(Time.unscaledTime * CursorPulseFrequency);
            _cursor.color = cursor;
        }
    }
}
