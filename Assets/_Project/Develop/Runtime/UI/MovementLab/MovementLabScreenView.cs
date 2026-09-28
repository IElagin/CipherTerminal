using System;
using Assets._Project.Develop.Runtime.UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.MovementLab
{
    public sealed class MovementLabScreenView : MonoBehaviour, IView
    {
        [SerializeField] private Button _backButton;

        public event Action BackClicked;

        private void OnEnable() => _backButton.onClick.AddListener(OnBackClicked);

        private void OnDisable() => _backButton.onClick.RemoveListener(OnBackClicked);

        private void OnBackClicked() => BackClicked?.Invoke();
    }
}
