using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Core
{
    public abstract class PopupViewBase : MonoBehaviour, IShowableView
    {
        private const float VisibleAlpha = 1;

        [SerializeField] private CanvasGroup _mainGroup;
        [SerializeField] private CanvasGroup _body;
        [SerializeField] private Image _anticlicker;
        [SerializeField] private PopupAnimationTypes _animationType;

        private Tween _currentAnimation;
        private float _anticlickerDefaultAlpha;

        public event Action CloseRequest;

        public void OnCloseButtonClicked() => CloseRequest?.Invoke();

        public Tween Show()
        {
            KillCurrentAnimation();
            OnPreShow();
            _mainGroup.alpha = VisibleAlpha;

            Sequence animation = PopupAnimationsCreator.CreateShowAnimation(
                _body, _anticlicker, _animationType, _anticlickerDefaultAlpha);

            ModifyShowAnimation(animation);
            // A callback keeps None executable and preserves the visual post hook.
            animation.AppendCallback(OnPostShow);

            return _currentAnimation = animation.SetUpdate(true).Play();
        }

        public Tween Hide()
        {
            KillCurrentAnimation();
            OnPreHide();

            Sequence animation = PopupAnimationsCreator.CreateHideAnimation(
                _body, _anticlicker, _animationType, _anticlickerDefaultAlpha);

            ModifyHideAnimation(animation);
            animation.AppendCallback(OnPostHide);

            return _currentAnimation = animation.SetUpdate(true).Play();
        }

        protected virtual void OnPreShow() { }

        protected virtual void OnPostShow() { }

        protected virtual void OnPreHide() { }

        protected virtual void OnPostHide() => _mainGroup.alpha = 0;

        protected virtual void ModifyShowAnimation(Sequence animation) { }

        protected virtual void ModifyHideAnimation(Sequence animation) { }

        private void Awake()
        {
            _mainGroup.alpha = 0;
            _anticlickerDefaultAlpha = _anticlicker.color.a;
        }

        private void OnDestroy() => KillCurrentAnimation();

        private void KillCurrentAnimation() => _currentAnimation?.Kill();
    }
}
