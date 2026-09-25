using Assets._Project.Develop.Runtime.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay
{
    public sealed class GameplayScreenView : MonoBehaviour, IView
    {
        private const float CursorVerticalOffset = -48;
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

        private GameplayFeedback _feedback;

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

        public void SetInputActive(bool active) => _feedback.SetInputActive(active, Time.unscaledTime);

        public void PlayInputFeedback() => _feedback.PlayInputFeedback(Time.unscaledTime);

        public void PlayErrorFeedback() => _feedback.PlayErrorFeedback(Time.unscaledTime);

        public void PlayVictoryFeedback() => _feedback.PlayVictoryFeedback(Time.unscaledTime);

        private void Awake()
        {
            _feedback = new GameplayFeedback(_rows, _pulse, _cursor, _sweep);
            _feedback.Reset();
        }

        private void OnDisable() => _feedback.Reset();

        private void Update() => _feedback.Tick(Time.unscaledTime);
    }
}
