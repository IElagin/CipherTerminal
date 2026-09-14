using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.UI
{
    public sealed class TerminalCell : MonoBehaviour
    {
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
            _border.color = done || current ? accent : new Color(.39f, .34f, .25f, 1);
            _fill.color = done ? new Color(.12f, .085f, .035f, 1f) : new Color(.06f, .055f, .04f, 1f);
        }
    }
}
