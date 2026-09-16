using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.UI
{
    public sealed class TerminalCell : MonoBehaviour
    {
        private static readonly Color _inactiveBorder = new Color(.39f, .34f, .25f, 1f);
        private static readonly Color _verifiedFill = new Color(.12f, .085f, .035f, 1f);
        private static readonly Color _pendingFill = new Color(.06f, .055f, .04f, 1f);

        [SerializeField] private TMP_Text _symbol;
        [SerializeField] private TMP_Text _marker;
        [SerializeField] private Image _border;
        [SerializeField] private Image _fill;

        public void Show(char symbol, int index, SequenceSession session)
        {
            bool done = index < session.Progress;
            bool current = index == session.Progress && session.State != SequenceState.Won;
            bool failed = current && session.State == SequenceState.Lost;
            Color accent = failed ? TerminalView.Error : TerminalView.Amber;

            _symbol.text = symbol.ToString();
            _symbol.color = done ? TerminalView.Amber : TerminalView.Ivory;
            _marker.text = done ? "ПРОВЕРЕНО" : failed ? "ОШИБКА" : current ? "ВВОД" : "";
            _marker.color = accent;
            _border.color = done || current ? accent : _inactiveBorder;
            _fill.color = done ? _verifiedFill : _pendingFill;
        }
    }
}
