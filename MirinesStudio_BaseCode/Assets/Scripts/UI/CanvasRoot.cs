using UnityEngine;

namespace MirinesStudio
{

    public class CanvasRoot : MonoBehaviour
    {
        // ----- Param -----

        public static CanvasRoot instance = null;

        [Header("UI Canvas Parents")]
        public Transform trScreenParent;
        public Transform trHudParent;
        public Transform trPopupParent;

        [Header("UI ADS")]
        public GameObject objAdsBannerFrame;

        [Header("UI ETC")]
        public TutorialHighlighter highlighter;

        // ----- Init -----

        private void Awake()
        {
            instance = this;
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----
    }
}