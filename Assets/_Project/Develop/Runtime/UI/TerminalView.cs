using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.UI
{
    public sealed class TerminalView : MonoBehaviour
    {
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

            float cellWidth = Mathf.Min(190f, (1280f - (session.Target.Length - 1) * 18f) / session.Target.Length);
            float total = session.Target.Length * (cellWidth + 18f) - 18f;

            for (int i = 0; i < _cells.Length; i++)
            {
                bool visible = i < session.Target.Length;
                _cells[i].gameObject.SetActive(visible);

                if (!visible)
                    continue;

                RectTransform rect = (RectTransform)_cells[i].transform;
                rect.sizeDelta = new Vector2(cellWidth, 215);
                rect.anchoredPosition = new Vector2(-total / 2 + cellWidth / 2 + i * (cellWidth + 18f), 0);
                _cells[i].Show(session.Target[i], i, session);
            }

            bool input = session.State == SequenceState.Input;
            bool won = session.State == SequenceState.Won;
            _outcome.text = input ? "" : won ? "ДОСТУП РАЗРЕШЁН" : "ДОСТУП ЗАПРЕЩЁН";
            _outcome.color = won ? Amber : Error;
            _hint.text = input ? "ВВЕДИТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ" : won ? "ПРОБЕЛ  /  ВЕРНУТЬСЯ В МЕНЮ" : "ПРОБЕЛ  /  ПОВТОРИТЬ ПРОТОКОЛ";
            _cursor.gameObject.SetActive(input);

            if (input)
                _cursor.rectTransform.anchoredPosition = new Vector2(-total / 2 + cellWidth / 2 + session.Progress * (cellWidth + 18f), -92);
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
            color.a = Mathf.Max(0, 1 - age / .28f) * (_error ? .10f : .045f);
            _pulse.color = color;

            if (_cellRow != null)
                _cellRow.anchoredPosition = new Vector2(_error && age < .25f ? Mathf.Sin(age * 90) * 5 * (1 - age / .25f) : 0, -5);

            if (_sweep != null)
            {
                bool sweeping = _state == SequenceState.Won && age >= 0 && age < .55f;
                _sweep.gameObject.SetActive(sweeping);

                if (sweeping)
                    _sweep.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-640, 640, age / .55f), 0);
            }

            if (_cursor != null)
            {
                Color cursor = Amber;
                cursor.a = .55f + .45f * Mathf.Sin(Time.unscaledTime * 4);
                _cursor.color = cursor;
            }
        }
    }
}
