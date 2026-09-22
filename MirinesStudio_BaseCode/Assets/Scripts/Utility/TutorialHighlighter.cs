/// 해당 스크립트는 Screen Space - Camera에서 동작하게 해놓음
/// 다른 세팅으로 수정할 경우 UtilFunction.GetPlaceUIAtWorld 부분 확인 필요

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

        private RectTransform _target;      // 강조할 대상

        // ----- Init -----

        // ----- Set -----

        public void Highlight(RectTransform t)
        {
            _target = t;
            overlay.gameObject.SetActive(true);
            UpdateHighlight();

            imgHighlight.sprite = null;
        }

        public void Highlight(RectTransform t, Sprite sprite)
        {
            _target = t;
            overlay.gameObject.SetActive(true);
            UpdateHighlight();

            if (imgHighlight != null && sprite == null)
                imgHighlight.sprite = sprite;
        }

        public void Hide()
        {
            overlay.gameObject.SetActive(false);
            _target = null;
        }

        // ----- Get -----

        // ----- Main -----


        //void LateUpdate()
        //{
        //    // 대상 UI가 움직일 수도 있으니 매 프레임 업데이트하고 싶으면 여기에
        //    if (target != null)
        //        UpdateHighlight();
        //}

        // ----- Logic -----

        void UpdateHighlight()
        {
            // 1) 타겟의 오버레이 로컬 좌표 코너 구하기
            Vector3[] worldCorners = new Vector3[4];
            _target.GetWorldCorners(worldCorners);

            Vector2[] localCorners = new Vector2[4];

            for (int i = 0; i < 4; i++)
            {
                // Screen Space - Camera에서는 반드시 uiCamera 사용
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(GameCore.UI.GetMainCam(), worldCorners[i]);
                localCorners[i] = UtilFunction.GetPlaceUIAtWorld(overlay, screenPos, GameCore.UI.GetMainCam());
            }

            Vector2 localMin = localCorners[0]; // bottom-left
            Vector2 localMax = localCorners[2]; // top-right

            float xMin = localMin.x;
            float xMax = localMax.x;
            float yMin = localMin.y;
            float yMax = localMax.y;

            Rect r = overlay.rect;
            float halfW = r.width * 0.5f;
            float halfH = r.height * 0.5f;

            float leftX = -halfW;
            float rightX = halfW;
            float bottomY = -halfH;
            float topY = halfH;

            // 위쪽 딤 : [yMax ~ top]
            SetRect(topDim,
                new Vector2(0, (topY + yMax) * 0.5f),                 // 중심
                new Vector2(r.width, Mathf.Max(0, topY - yMax)));     // 크기

            // 아래 딤 : [bottom ~ yMin]
            SetRect(bottomDim,
                new Vector2(0, (bottomY + yMin) * 0.5f),
                new Vector2(r.width, Mathf.Max(0, yMin - bottomY)));

            // 왼쪽 딤 : [left ~ xMin], 세로는 [yMin ~ yMax]만
            SetRect(leftDim,
                new Vector2((leftX + xMin) * 0.5f, (yMin + yMax) * 0.5f),
                new Vector2(Mathf.Max(0, xMin - leftX), Mathf.Max(0, yMax - yMin)));

            // 오른쪽 딤 : [xMax ~ right], 세로는 [yMin ~ yMax]만
            SetRect(rightDim,
                new Vector2((rightX + xMax) * 0.5f, (yMin + yMax) * 0.5f),
                new Vector2(Mathf.Max(0, rightX - xMax), Mathf.Max(0, yMax - yMin)));

            // 3) 가운데 하이라이트 테두리
            if (highlightFrame != null)
            {
                SetRect(highlightFrame,
                    new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f),
                    new Vector2(xMax - xMin, yMax - yMin));
            }

            void SetRect(RectTransform rt, Vector2 center, Vector2 size)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                rt.anchoredPosition = center;
                rt.sizeDelta = size;
            }
        }
    }
}