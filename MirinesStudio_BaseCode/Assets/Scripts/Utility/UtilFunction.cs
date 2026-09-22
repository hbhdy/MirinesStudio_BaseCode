using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace MirinesStudio
{
	public static class UtilFunction 
	{

        // uiCam = null로 하면 Screen Overlay로 처리됨 
        public static Vector2 GetPlaceUIAtWorld(RectTransform rect, Vector2 screenPos, Camera uiCam = null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, uiCam, out localPoint);
            return localPoint;
        }

        public static void CreateInstantiateUIObject(out GameObject objTarget, GameObject objPrefab, GameObject objParent)
        {
            objTarget = GameObject.Instantiate(objPrefab, objParent.transform, false) as GameObject;
            objTarget.name = objPrefab.name;

            SetRectTransform(objTarget, objPrefab);
        }

        public static void CreateInstantiateUIObject(out GameObject objTarget, GameObject objPrefab, Transform trParent)
        {
            objTarget = GameObject.Instantiate(objPrefab, trParent, false) as GameObject;
            objTarget.name = objPrefab.name;

            SetRectTransform(objTarget, objPrefab);
        }

        public static void SetRectTransform(GameObject objTarget, GameObject objOrigin)
        {
            SetRectTransform(objTarget.GetComponent<RectTransform>(), objOrigin.GetComponent<RectTransform>());
        }

        public static void SetRectTransform(RectTransform rectTarget, RectTransform rectOrigin)
        {
            if (rectTarget == null || rectOrigin == null)
                return;

            rectTarget.anchoredPosition = rectOrigin.anchoredPosition;
            rectTarget.anchoredPosition3D = rectOrigin.anchoredPosition3D;
            rectTarget.anchorMax = rectOrigin.anchorMax;
            rectTarget.anchorMin = rectOrigin.anchorMin;
            rectTarget.offsetMax = rectOrigin.offsetMax;
            rectTarget.offsetMin = rectOrigin.offsetMin;
            rectTarget.pivot = rectOrigin.pivot;
            rectTarget.sizeDelta = rectOrigin.sizeDelta;
            rectTarget.localScale = rectOrigin.localScale;
        }

        public static void SetActiveCheck(GameObject obj, bool isActive)
        {
            if (obj != null && obj.activeSelf != isActive)
                obj.SetActive(isActive);
        }

        public static string GetFullPath(Transform tr, string path = null)
        {
            StringBuilder sb = new StringBuilder(path ?? string.Empty);
            Transform t = tr;

            while (t != null)
            {
                sb.Insert(0, $"{t.name}/");
                t = t.parent;
            }

            return sb.ToString().TrimEnd('/');
        }

        public static bool IsServerVersionNewer(string localVersion, string serverVersion)
        {
            if (string.IsNullOrEmpty(localVersion) || string.IsNullOrEmpty(serverVersion))
                return false;

            int[] local = ParseVersion(localVersion);
            int[] server = ParseVersion(serverVersion);

            // 배열 길이 (3) 기준
            for (int i = 0; i < 3; i++)
            {
                if (server[i] > local[i])
                    return true;   // 서버 버전이 더 높으면 업데이트 필요
                if (server[i] < local[i])
                    return false;  // 클라이언트가 더 높으면 업데이트 불필요
            }

            return false; // 완전히 같으면 false
        }

        private static int[] ParseVersion(string version)
        {
            string[] parts = version.Split('.');
            int[] numbers = new int[3] { 0, 0, 0 };

            for (int i = 0; i < parts.Length && i < 3; i++)
            {
                int.TryParse(parts[i], out numbers[i]);
            }

            return numbers;
        }

        public static string ReplaceNewLine(this object obj) => obj.ToString().Replace("\\n", System.Environment.NewLine).Replace("\n", System.Environment.NewLine);

        public static Color HexColor(string hexCode)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(hexCode, out color))
            {
                return color;
            }
            
            MSLog.LogError("[HexColor]invalid hex code - " + hexCode);
            return Color.white;
        }

        #region Number Count Up & Down

        public static Tween AnimateScore(Tween tween, TextMeshProUGUI text, float startScore, float targetScore, float duration)
        {
            // 이전 트윈이 실행 중이면 중단
            tween?.Kill();

            float currentValue = startScore;

            tween = DOTween.To(
                        () => currentValue,
                        x =>
                        {
                            currentValue = x;
                            text.text = currentValue.ToString("F2"); // 소수점 둘째 자리 고정
                        },
                        targetScore,
                        duration
                    )
                    .SetEase(Ease.OutCubic);

            return tween;
        }

        public static Tween AnimateScore(Tween tween, TextMeshProUGUI text, int startScore, int targetScore, float duration)
        {
            // 이전 트윈이 실행 중이면 중단
            tween?.Kill();

            // 현재 값 저장용 임시 변수
            long currentValue = startScore;

            // 트윈 시작
            tween = DOTween.To(() => (float)currentValue,
                                    x => {
                                        currentValue = (int)x;
                                        text.text = currentValue.ToString(); // 1,000 단위 콤마 표시
                                    },
                                    targetScore,
                                    duration)
                                .SetEase(Ease.OutCubic);

            return tween;
        }

        #endregion

        #region Coroutine Timer

        // 종료 시간 동안 타이머 동작 ( 리얼 타임 )
        public static IEnumerator SetRemainTimeText_EndTime(TextMeshProUGUI textObject, DateTime endTime, Action timeOverAction = null)
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
            TimeSpan remainTime;

            remainTime = endTime - DateTime.UtcNow;

            while (remainTime.TotalSeconds > 0)
            {
                remainTime = endTime - DateTime.UtcNow;

                if (remainTime.TotalSeconds < 0)
                {
                    textObject.text = $"";
                    timeOverAction?.Invoke();
                    break;
                }

                textObject.text = GetTimeString(remainTime, string.Empty, remainTime.TotalHours > 1);

                yield return wait;
            }
        }

        public static string GetTimeString(TimeSpan remainTime, string format = "", bool isHourDisplay = true)
        {
            return isHourDisplay
                ? $"{format}{Math.Floor(remainTime.TotalHours)}:{remainTime.Minutes:D2}:{remainTime.Seconds:D2}"
                : $"{format}{remainTime.Minutes:D2}:{remainTime.Seconds:D2}";
        }

        #endregion

        #region Find Object

        private static Transform FindTransform(Transform tr, string path)
        {
            Transform t = tr.Find(path);
            if (t == null)
            {
                MSLog.LogWarning($"Child Not Found : path ={GetFullPath(tr, path)}");
                return null;
            }

            return t;
        }

        public static GameObject Find(Transform tr, string path)
        {
            Transform t = FindTransform(tr, path);
            return t == null ? null : t.gameObject;
        }

        public static T Find<T>(Transform tr) where T : Component
        {
            if (tr.TryGetComponent(out T t))
                return t;

            MSLog.LogWarning($"{typeof(T).Name} Not Found : path={GetFullPath(tr, "")}");
            return null;
        }

        public static T Find<T>(Transform tr, string path) where T : Component
        {
            Transform trans = FindTransform(tr, path);

            if (trans.TryGetComponent(out T t))
                return t;

            MSLog.LogWarning($"{typeof(T).Name} Not Found : path={GetFullPath(tr, path)}");
            return null;
        }

        public static GameObject Find(this GameObject obj, string path)
        {
            return Find(obj.transform, path);
        }

        public static T Find<T>(this GameObject obj, string path) where T : Component
        {
            return Find<T>(obj.transform, path);
        }

        public static GameObject Find(Component component, string path)
        {
            return Find(component.transform, path);
        }

        public static T Find<T>(Component component, string path) where T : Component
        {
            return Find<T>(component.transform, path);
        }

        public static List<GameObject> FindAll(Transform tr, string path)
        {
            List<GameObject> all = new List<GameObject>();
            Transform findTr = FindTransform(tr, path);

            if (findTr?.parent != null)
            {
                foreach (Transform childTr in findTr.parent)
                {
                    if (childTr.name == findTr.name)
                        all.Add(childTr.gameObject);
                }
            }

            return all;
        }

        public static List<T> FindAll<T>(Transform tr, string path) where T : Component
        {
            List<T> all = new List<T>();
            foreach (GameObject obj in FindAll(tr, path))
            {
                T component = obj.GetComponent<T>();
                if (component != null)
                    all.Add(component);
            }

            return all;
        }

        public static List<GameObject> FindAll(this GameObject obj, string path)
        {
            return FindAll(obj.transform, path);
        }

        public static List<T> FindAll<T>(this GameObject obj, string path) where T : Component
        {
            return FindAll<T>(obj.transform, path);
        }

        public static T FindInParents<T>(this GameObject obj) where T : Component
        {
            if (obj == null)
                return null;

            Transform tr = obj.transform;
            while (tr != null)
            {
                if (tr.TryGetComponent(out T comp))
                    return comp;

                tr = tr.parent;
            }

            return null;
        }

        public static GameObject FindParentObject(Transform target)
        {
            return target?.parent?.gameObject ?? null;
        }

        public static GameObject FindChild(GameObject obj, string strName)
        {
            if (obj == null)
                return null;

            if (obj.name == strName)
                return obj;

            Transform tr = obj.transform.Find(strName);
            if (tr != null)
                return tr.gameObject;

            foreach (Transform chlidTr in obj.transform)
            {
                if (chlidTr == null)
                    continue;

                GameObject returnObj = FindChild(chlidTr.gameObject, strName);
                if (returnObj != null)
                    return returnObj;
            }

            return null;
        }

        #endregion Find Object

        #region Android Setting

#if UNITY_ANDROID
        public static class AndroidOsBuild
        {
            public static int SDK_INT
            {
                get
                {
#if UNITY_EDITOR
                    return 99;
#else
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        return version.GetStatic<int>("SDK_INT");
                    }
#endif
                }
            }

            public static int GetVersionCode(string version)
            {
                using (var versionCodes = new AndroidJavaClass("android.os.Build$VERSION_CODES"))
                {
                    return versionCodes.GetStatic<int>(version);
                }
            }
        }
#endif

        #endregion
    }
}