using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI
{
    public sealed class WalletPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _gold;
        [SerializeField] private TMP_Text _wins;
        [SerializeField] private TMP_Text _losses;
        [SerializeField] private TMP_Text _resetCost;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private GameObject _expandedRoot;
        [SerializeField] private RectTransform _chevron;

        public Button ToggleButton => _toggleButton;
        public Button ResetButton => _resetButton;
        public bool IsExpanded => _expandedRoot.activeSelf;

        public void Render(int gold, int wins, int losses, int resetCost)
        {
            _gold.text = gold.ToString();
            _wins.text = wins.ToString();
            _losses.text = losses.ToString();
            _resetCost.text = resetCost + " золота";
        }

        public void SetExpanded(bool expanded)
        {
            _expandedRoot.SetActive(expanded);
            _chevron.localRotation = Quaternion.Euler(0f, 0f, expanded ? 180f : 0f);
        }

        public void SetInteractable(bool interactable)
        {
            _toggleButton.interactable = interactable;
            _resetButton.interactable = interactable;
        }

        public void ShowStatus(string text, bool error = false)
        {
            _status.text = text;
            _status.color = error ? TerminalView.Error : TerminalView.Amber;
        }

        public void ShowUnavailable(string message)
        {
            _gold.text = "—";
            _wins.text = "—";
            _losses.text = "—";
            _resetCost.text = "—";
            ShowStatus(message, true);
        }
    }
}
