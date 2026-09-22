using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace MirinesStudio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ----- Param -----

        [Header("Target")]
        [SerializeField] private RectTransform target;

        [Header("Press")]
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float pressDuration = 0.08f;
        [SerializeField] private Ease pressEase = Ease.OutQuad;

        [Header("Release")]
        [SerializeField] private float releaseDuration = 0.10f;
        [SerializeField] private Ease releaseEase = Ease.OutQuad;

        [Header("Click Pop (optional)")]
        [SerializeField] private bool enableClickPop = true;
        [SerializeField] private float popScale = 1.05f;
        [SerializeField] private float popDuration = 0.06f;
        [SerializeField] private Ease popEase = Ease.OutQuad;

        [Header("Cooldown (optional)")]
        [SerializeField] private bool enableCooldown = false;
        [SerializeField] private float cooldownSeconds = 0.10f;

        [Header("Options")]
        [Tooltip("UI는 보통 unscaled로 연출하는 게 자연스러움")]
        [SerializeField] private bool useUnscaledTime = true;

        private Selectable selectable;
        private Vector3 baseScale;
        private Tween scaleTween;
        private Sequence clickSeq;
        private float nextAllowedTime;

        // ----- Init -----
        private void Awake()
        {
            selectable = GetComponent<Selectable>();

            if (!target)
                target = transform as RectTransform;

            baseScale = target.localScale;
        }

        private void OnDisable()
        {
            KillTweens();

            if (target)
                target.localScale = baseScale;
        }

        private void KillTweens()
        {
            if (scaleTween != null && scaleTween.IsActive())
                scaleTween.Kill();

            if (clickSeq != null && clickSeq.IsActive())
                clickSeq.Kill();

            scaleTween = null;
            clickSeq = null;
        }

        // ----- Set -----

        public void RefreshBaseScale()
        {
            if (!target) 
                return;

            baseScale = target.localScale;
        }

        // ----- Get -----

        private bool IsInteractable()
        {
            if (!selectable || !selectable.IsInteractable())
                return false;

            if (enableCooldown && Time.unscaledTime < nextAllowedTime)
                return false;

            return true;
        }

        // ----- Main -----

        // ----- Logic -----

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
                return;

            PlayScale(baseScale * pressScale, pressDuration, pressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!selectable || !selectable.IsInteractable())
                return;

            PlayScale(baseScale, releaseDuration, releaseEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 누른 채로 밖으로 나가면 원복
            PlayScale(baseScale, releaseDuration, releaseEase);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable())
                return;

            if (enableCooldown)
                nextAllowedTime = Time.unscaledTime + cooldownSeconds;

            if (!enableClickPop)
                return;

            if (clickSeq != null && clickSeq.IsActive())
                clickSeq.Kill();

            if (scaleTween != null && scaleTween.IsActive())
                scaleTween.Kill();

            clickSeq = DOTween.Sequence();
            clickSeq.SetUpdate(useUnscaledTime);

            clickSeq.Append(target.DOScale(baseScale * popScale, popDuration).SetEase(popEase));
            clickSeq.Append(target.DOScale(baseScale, popDuration).SetEase(releaseEase));
        }

        private void PlayScale(Vector3 to, float duration, Ease ease)
        {
            if (!target)
                return;

            if (clickSeq != null && clickSeq.IsActive())
                clickSeq.Kill();

            if (scaleTween != null && scaleTween.IsActive())
                scaleTween.Kill();

            scaleTween = target
                .DOScale(to, duration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime);
        }
    }
}