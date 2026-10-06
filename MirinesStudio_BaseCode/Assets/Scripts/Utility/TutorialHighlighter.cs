using UnityEngine;
using UnityEngine.UI;

namespace MirinesStudio
{
    public class TutorialHighlighter : MonoBehaviour
    {
        // ----- Param -----

        [Header("오브젝트")]
        public RectTransform overlay;
        public RectTransform topDim;
        public RectTransform bottomDim;
        public RectTransform leftDim;
        public RectTransform rightDim;
        public RectTransform highlightFrame;
        public Image imgHighlight;

        private RectTransform _target;
        private bool isHighlighting;
        private readonly Vector3[] worldCorners = new Vector3[4];

        // ----- Set -----

        public void Highlight(RectTransform t) => Highlight(t, null);

        public void Highlight(RectTransform t, Sprite sprite)
        {
            if (t == null || !t.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            _target = t;
            isHighlighting = true;
            overlay.gameObject.SetActive(true);
            if (imgHighlight != null)
                imgHighlight.sprite = sprite;
            Canvas.ForceUpdateCanvases();
            UpdateHighlight();
        }

        public void Hide()
        {
            isHighlighting = false;
            _target = null;
            overlay.gameObject.SetActive(false);
        }

        // ----- Main -----

        private void LateUpdate()
        {
            if (!isHighlighting)
                return;
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }
            UpdateHighlight();
        }

        // ----- Logic -----

        private void UpdateHighlight()
        {
            Camera sourceCamera = GetCanvasCamera(_target);
            Camera destinationCamera = GetCanvasCamera(overlay);
            _target.GetWorldCorners(worldCorners);
            Vector2 localMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 localMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldCorners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screenPos, destinationCamera, out Vector2 localPos))
                    return;
                localMin = Vector2.Min(localMin, localPos);
                localMax = Vector2.Max(localMax, localPos);
            }

            Rect r = overlay.rect;
            float xMin = Mathf.Clamp(localMin.x, r.xMin, r.xMax);
            float xMax = Mathf.Clamp(localMax.x, r.xMin, r.xMax);
            float yMin = Mathf.Clamp(localMin.y, r.yMin, r.yMax);
            float yMax = Mathf.Clamp(localMax.y, r.yMin, r.yMax);

            SetRect(topDim, new Vector2(r.center.x, (r.yMax + yMax) * 0.5f), new Vector2(r.width, r.yMax - yMax));
            SetRect(bottomDim, new Vector2(r.center.x, (r.yMin + yMin) * 0.5f), new Vector2(r.width, yMin - r.yMin));
            SetRect(leftDim, new Vector2((r.xMin + xMin) * 0.5f, (yMin + yMax) * 0.5f), new Vector2(xMin - r.xMin, yMax - yMin));
            SetRect(rightDim, new Vector2((r.xMax + xMax) * 0.5f, (yMin + yMax) * 0.5f), new Vector2(r.xMax - xMax, yMax - yMin));
            if (highlightFrame != null)
                SetRect(highlightFrame, (localMin + localMax) * 0.5f, localMax - localMin);
        }

        private static Camera GetCanvasCamera(RectTransform target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;
            Canvas rootCanvas = canvas.rootCanvas;
            return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }

        private void SetRect(RectTransform rt, Vector2 center, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = overlay.pivot;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = size;
        }
    }
}
