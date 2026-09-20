using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Utilities.AssetsManagement;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Core
{
    public sealed class ViewsFactory
    {
        private readonly ResourcesAssetLoader _resources;
        private readonly Dictionary<string, string> _paths = new()
        {
            { ViewIDs.ResetStatisticsPopup, "UI/ResetStatisticsPopup" }
        };

        public ViewsFactory(ResourcesAssetLoader resources)
        {
            _resources = resources;
        }

        public TView Create<TView>(string viewId, Transform parent) where TView : MonoBehaviour, IView
        {
            if (_paths.TryGetValue(viewId, out string path) == false)
                throw new ArgumentException("Unknown view: " + viewId, nameof(viewId));
            GameObject prefab = _resources.Load<GameObject>(path);

            if (prefab == null || prefab.GetComponent<TView>() == null)
                throw new InvalidOperationException($"View prefab {path} must contain {typeof(TView).Name}");

            return UnityEngine.Object.Instantiate(prefab, parent).GetComponent<TView>();
        }

        public void Release(PopupViewBase view) => UnityEngine.Object.Destroy(view.gameObject);
    }
}
