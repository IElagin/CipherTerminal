using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.UI
{
    public sealed class TerminalView : MonoBehaviour
    {
        private const float MaximumCellWidth = 190f;
        private const float AvailableRowWidth = 1280f;
        private const float CellSpacing = 18f;
        private const float CellHeight = 215f;
        private const float CenterFraction = .5f;
        private const float CursorVerticalOffset = -92f;
        private const float RowVerticalOffset = -5f;
        private const float PulseDuration = .28f;
        private const float ErrorPulseOpacity = .10f;
        private const float InputPulseOpacity = .045f;
        private const float ShakeDuration = .25f;
        private const float ShakeFrequency = 90f;
        private const float ShakeAmplitude = 5f;
        private const float SweepDuration = .55f;
        private const float SweepHalfWidth = 640f;
        private const float CursorBaseOpacity = .55f;
        private const float CursorOpacityAmplitude = .45f;
        private const float CursorPulseFrequency = 4f;
        private const float FullOpacity = 1f;
        private const int TrailingGapCount = 1;

        public static readonly Color Amber = new Color(1f, .69f, .23f);
        public static readonly Color Ivory = new Color(.91f, .86f, .73f);
        public static readonly Color Error = new Color(1f, .35f, .2f);

        [SerializeField] private Button _digitsButton;
        [SerializeField] private Button _lettersButton;
        [SerializeField] private TMP_Text _protocol;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private TMP_Text _outcome;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private TerminalCell[] _cells;
        [SerializeField] private RectTransform _cellRow;
        [SerializeField] private Image _pulse;
        [SerializeField] private Image _cursor;
        [SerializeField] private Image _sweep;

        private float _pulseStart = -10f;
        private bool _error;
        private SequenceState _state;

        public Button DigitsButton => _digitsButton;
        public Button LettersButton => _lettersButton;

        public void SetMenuEnabled(bool enabled)
        {
            _digitsButton.interactable = enabled;
            _lettersButton.interactable = enabled;
        }

        public void ShowSequence(SequenceSession session, SequenceMode mode)
        {
            _state = session.State;
            _protocol.text = mode == SequenceMode.Digits ? "ЦИФРОВОЙ ПРОТОКОЛ" : "БУКВЕННЫЙ ПРОТОКОЛ";
            _progress.text = session.Progress + " / " + session.Target.Length + "  ПРОВЕРЕНО";

            float cellWidth = Mathf.Min(MaximumCellWidth,
                (AvailableRowWidth - (session.Target.Length - TrailingGapCount) * CellSpacing) / session.Target.Length);
            float totalWidth = session.Target.Length * (cellWidth + CellSpacing) - CellSpacing;

            for (int i = 0; i < _cells.Length; i++)
            {
                bool visible = i < session.Target.Length;
                _cells[i].gameObject.SetActive(visible);

                if (visible == false)
                    continue;

                RectTransform rect = (RectTransform)_cells[i].transform;
                rect.sizeDelta = new Vector2(cellWidth, CellHeight);
                rect.anchoredPosition = new Vector2(GetCellPosition(i, cellWidth, totalWidth), 0);
                _cells[i].Show(session.Target[i], i, session);
            }

            bool input = session.State == SequenceState.Input;
            bool won = session.State == SequenceState.Won;
            _outcome.text = input ? "" : won ? "ДОСТУП РАЗРЕШЁН" : "ДОСТУП ЗАПРЕЩЁН";
            _outcome.color = won ? Amber : Error;
            _hint.text = input ? "ВВЕДИТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ" : won ? "ПРОБЕЛ  /  ВЕРНУТЬСЯ В МЕНЮ" : "ПРОБЕЛ  /  ПОВТОРИТЬ ПРОТОКОЛ";
            _cursor.gameObject.SetActive(input);

            if (input)
                _cursor.rectTransform.anchoredPosition = new Vector2(
                    GetCellPosition(session.Progress, cellWidth, totalWidth), CursorVerticalOffset);
        }

        public void Pulse(bool error)
        {
            _pulseStart = Time.unscaledTime;
            _error = error;
        }

        private void Update()
        {
            if (_pulse == null)
                return;

            float age = Time.unscaledTime - _pulseStart;
            Color color = _error ? Error : Amber;
            color.a = Mathf.Max(0, FullOpacity - age / PulseDuration) * (_error ? ErrorPulseOpacity : InputPulseOpacity);
            _pulse.color = color;

            if (_cellRow != null)
            {
                float horizontalShake = _error && age < ShakeDuration
                    ? Mathf.Sin(age * ShakeFrequency) * ShakeAmplitude * (FullOpacity - age / ShakeDuration)
                    : 0;
                _cellRow.anchoredPosition = new Vector2(horizontalShake, RowVerticalOffset);
            }

            if (_sweep != null)
            {
                bool sweeping = _state == SequenceState.Won && age >= 0 && age < SweepDuration;
                _sweep.gameObject.SetActive(sweeping);

                if (sweeping)
                    _sweep.rectTransform.anchoredPosition = new Vector2(
                        Mathf.Lerp(-SweepHalfWidth, SweepHalfWidth, age / SweepDuration), 0);
            }

            if (_cursor != null)
            {
                Color cursor = Amber;
                cursor.a = CursorBaseOpacity + CursorOpacityAmplitude * Mathf.Sin(Time.unscaledTime * CursorPulseFrequency);
                _cursor.color = cursor;
            }
        }

        private static float GetCellPosition(int index, float cellWidth, float totalWidth)
            => -totalWidth * CenterFraction + cellWidth * CenterFraction + index * (cellWidth + CellSpacing);
    }
}
