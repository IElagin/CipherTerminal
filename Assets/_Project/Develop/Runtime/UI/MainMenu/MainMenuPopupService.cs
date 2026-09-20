using Assets._Project.Develop.Runtime.UI.Core;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.MainMenu
{
    public sealed class MainMenuPopupService : PopupService
    {
        private readonly UIRoot _root;

        public MainMenuPopupService(ViewsFactory viewsFactory, ProjectPresentersFactory presentersFactory,
            UIRoot root) : base(viewsFactory, presentersFactory) => _root = root;

        protected override Transform PopupLayer => _root.PopupsLayer;
    }
}
