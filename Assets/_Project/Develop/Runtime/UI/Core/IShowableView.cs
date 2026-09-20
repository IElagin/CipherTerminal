using DG.Tweening;

namespace Assets._Project.Develop.Runtime.UI.Core
{
    public interface IShowableView : IView
    {
        public Tween Show();

        public Tween Hide();
    }
}
