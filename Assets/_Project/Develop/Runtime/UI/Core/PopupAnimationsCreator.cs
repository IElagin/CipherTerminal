using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Core
{
    public static class PopupAnimationsCreator
    {
        private const float BackgroundFadeDuration = 0.2f;
        private const float BodyScaleDuration = 0.5f;
        private const float VisibleScale = 1;
        private const float FadeDuration = .15f;
        private const float VisibleAlpha = 1;

        public static Sequence CreateShowAnimation(CanvasGroup body, Image anticlicker,
            PopupAnimationTypes animationType, float anticlickerMaxAlpha)
        {
            switch (animationType)
            {
                case PopupAnimationTypes.None:
                    return DOTween.Sequence();

                case PopupAnimationTypes.Fade:
                    body.alpha = 0;
                    return DOTween.Sequence()
                        .Append(body.DOFade(VisibleAlpha, FadeDuration))
                        .Join(anticlicker.DOFade(anticlickerMaxAlpha, FadeDuration).From(0));

                case PopupAnimationTypes.Expand:
                    return DOTween.Sequence()
                        .Append(anticlicker.DOFade(anticlickerMaxAlpha, BackgroundFadeDuration).From(0))
                        .Join(body.transform.DOScale(VisibleScale, BodyScaleDuration).From(0).SetEase(Ease.OutBack));

                default:
                    throw new ArgumentException(nameof(animationType));
            }
        }

        public static Sequence CreateHideAnimation(CanvasGroup body, Image anticlicker,
            PopupAnimationTypes animationType, float anticlickerMaxAlpha)
        {
            if (animationType == PopupAnimationTypes.Fade)
                return DOTween.Sequence().Append(body.DOFade(0, FadeDuration)).Join(anticlicker.DOFade(0, FadeDuration));

            return DOTween.Sequence();
        }
    }
}
