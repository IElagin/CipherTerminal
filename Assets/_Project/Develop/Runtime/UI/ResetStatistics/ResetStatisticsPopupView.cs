using System;
using Assets._Project.Develop.Runtime.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.ResetStatistics
{
    public sealed class ResetStatisticsPopupView : PopupViewBase
    {
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private TMP_Text _balance;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _confirm;
        [SerializeField] private Button _cancel;
        [SerializeField] private Button _close;
        [SerializeField] private Button _background;

        public event Action ConfirmClicked;

        public void Render(int cost, int balance)
        {
            _cost.text = "СБРОСИТЬ ЗА " + cost + " ЗОЛОТА";
            _balance.text = "БАЛАНС: " + balance + " золота";
        }

        public void SetInteraction(bool canConfirm, bool canClose)
        {
            _confirm.interactable = canConfirm;
            _cancel.interactable = canClose;
            _close.interactable = canClose;
            _background.interactable = canClose;
        }

        public void ShowStatus(string message) => _status.text = message;

        private void OnEnable()
        {
            _confirm.onClick.AddListener(OnConfirmClicked);
            _cancel.onClick.AddListener(OnCloseButtonClicked);
            _close.onClick.AddListener(OnCloseButtonClicked);
            _background.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDisable()
        {
            _confirm.onClick.RemoveListener(OnConfirmClicked);
            _cancel.onClick.RemoveListener(OnCloseButtonClicked);
            _close.onClick.RemoveListener(OnCloseButtonClicked);
            _background.onClick.RemoveListener(OnCloseButtonClicked);
        }

        private void OnConfirmClicked() => ConfirmClicked?.Invoke();
    }
}
