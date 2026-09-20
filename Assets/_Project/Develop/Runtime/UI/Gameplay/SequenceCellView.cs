using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay
{
    public sealed class SequenceCellView : MonoBehaviour
    {
        private static readonly Color _inactiveBorder = new Color(.39f, .34f, .25f);
        private static readonly Color _verifiedFill = new Color(.12f, .085f, .035f);
        private static readonly Color _pendingFill = new Color(.06f, .055f, .04f);

        [SerializeField] private TMP_Text _symbol;
        [SerializeField] private TMP_Text _marker;
        [SerializeField] private Image _border;
        [SerializeField] private Image _fill;
        [SerializeField] private GameObject _spaceGlyph;

        public void Render(string symbol, string marker, bool verified, bool current, bool error)
        {
            Color accent = error ? TerminalPalette.Error : TerminalPalette.Amber;
            _spaceGlyph.SetActive(symbol == "␣");
            _symbol.text = symbol == "␣" ? "" : symbol;
            _symbol.color = error || verified ? accent : TerminalPalette.Ivory;
            _marker.text = marker;
            _marker.color = accent;
            _border.color = error || verified || current ? accent : _inactiveBorder;
            _fill.color = verified ? _verifiedFill : _pendingFill;
        }
    }
}
