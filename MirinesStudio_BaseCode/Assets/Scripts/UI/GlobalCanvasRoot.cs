using UnityEngine;
using UnityEngine.UI;

namespace MirinesStudio
{
    public class GlobalCanvasRoot : MonoBehaviour
    {
        public Canvas globalCanvas;
        public Transform trPopupParent;
        public TutorialHighlighter highlighter;
        public Canvas loadingCanvas;
        public Slider loadingProgress;
        public Canvas inputBlockerCanvas;

        public bool IsTransitioning { get; private set; }

        public void Init()
        {
            globalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            globalCanvas.sortingOrder = (int)Canvas_SortOrder.GLOBAL;
            SetCanvasOrder(loadingCanvas, 30000);
            SetCanvasOrder(inputBlockerCanvas, 31000);
            if (highlighter != null)
            {
                SetCanvasOrder(highlighter.GetComponentInParent<Canvas>(), 20000);
                highlighter.Hide();
            }
            SetLoadingActive(false);
            SetTransitionActive(false);
        }

        public void SetTransitionActive(bool active)
        {
            IsTransitioning = active;
            if (active && highlighter != null)
                highlighter.Hide();
            if (inputBlockerCanvas != null)
                inputBlockerCanvas.gameObject.SetActive(active);
        }

        public void SetLoadingActive(bool active)
        {
            if (loadingCanvas != null)
                loadingCanvas.gameObject.SetActive(active);
        }

        public void SetLoadingProgress(float progress)
        {
            if (loadingProgress != null)
                loadingProgress.normalizedValue = progress;
        }

        private void SetCanvasOrder(Canvas canvas, int order)
        {
            if (canvas == null || canvas == globalCanvas)
                return;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = globalCanvas.sortingLayerID;
            canvas.sortingOrder = order;
        }
    }
}
