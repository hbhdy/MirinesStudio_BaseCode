using System.Collections.Generic;
using UnityEngine;

namespace MirinesStudio
{
    public class CameraScaler : MonoBehaviour
    {
        // ----- Param -----

        public enum FitMode { ConstantWidth, ConstantHeight }

        [Header("Portrait 기준 (width : height)")]
        [Tooltip("세로 게임이면 9:19.5 같은 식으로, '가로/세로' 비율을 넣으세요.")]
        public float referenceAspect = 9f / 19.5f;   //  0.4615 (세로 기준)

        [Header("기준 카메라 size (world units)")]
        [Tooltip("referenceAspect 기기에서의 orthographicSize")]
        public float referenceSize = 5f;

        [Header("맞춤 모드")]
        public FitMode fitMode = FitMode.ConstantWidth;  // 권장: ConstantWidth

        public float curSize;
        private Camera cam;
        // ----- Init -----


        void Awake()
        {
            cam = GetComponent<Camera>();
            ApplyScale();
        }

        void ApplyScale()
        {
            float currentAspect = (float)Screen.width / Screen.height; // 가로/세로

            if (fitMode == FitMode.ConstantWidth)
            {
                // 기준 기기에서의 '월드 가로폭'을 고정:
                // worldWidth_ref = 2 * referenceSize * referenceAspect
                float worldWidthRef = 2f * referenceSize * referenceAspect;
                // 현재 기기에서 그 가로폭을 보여주려면 size = worldWidth_ref / (2 * currentAspect)
                curSize = worldWidthRef / (2f * currentAspect);
            }
            else // ConstantHeight
            {
                // 세로 높이를 항상 동일하게 유지
                curSize = referenceSize;
            }

            cam.orthographicSize = curSize;
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

    }
}