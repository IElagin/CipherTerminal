using System;
using Assets._Project.Develop.Runtime.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.MainMenu
{
    public sealed class ProgressPanelView : MonoBehaviour, IView
    {
        [SerializeField] private TMP_Text _gold;
        [SerializeField] private TMP_Text _wins;
        [SerializeField] private TMP_Text _losses;
        [SerializeField] private TMP_Text _resetCost;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _resetButton;

        public event Action ResetClicked;

        public void Render(int gold, int wins, int losses, int resetCost)
        {
            _gold.text = gold.ToString();
            _wins.text = wins.ToString();
            _losses.text = losses.ToString();
            _resetCost.text = resetCost + " золота";
        }

        public void ShowUnavailable()
        {
            _gold.text = "—";
            _wins.text = "—";
            _losses.text = "—";
            _resetCost.text = "—";
        }

        public void SetInteractable(bool enabled) => _resetButton.interactable = enabled;

        public void ShowStatus(string message, bool error)
        {
            _status.text = message;
            _status.color = error ? TerminalPalette.Error : TerminalPalette.Amber;
        }

        private void OnEnable() => _resetButton.onClick.AddListener(OnResetClicked);

        private void OnDisable() => _resetButton.onClick.RemoveListener(OnResetClicked);

        private void OnResetClicked() => ResetClicked?.Invoke();
    }
}
