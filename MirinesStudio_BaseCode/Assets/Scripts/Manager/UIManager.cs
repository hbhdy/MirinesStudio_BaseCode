using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace MirinesStudio
{
    public enum UIAssetSource
    {
        Addressables,
        Resources
    }

    public class UIManager : BaseManager
    {
        // ----- Param -----

        private Transform trUIScreen;
        private Transform trUIPopup;
        private Transform trUIHud;

        private RectTransform rectUIScreen;
        private RectTransform rectUIPopup;
        private RectTransform rectUIHud;

        private Camera mainCam;

#if ADDRESSABLES
        // Addressables 인스턴스 해제용
        private readonly Dictionary<UIType, AsyncOperationHandle<GameObject>> dicInstHandle = new Dictionary<UIType, AsyncOperationHandle<GameObject>>();
#endif

        private readonly Dictionary<UIType, UIBase> dicUI = new Dictionary<UIType, UIBase>();
        private readonly List<UIBase> openUIList = new List<UIBase>();

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            SetMainCamera();
            SetTrUIParent(CanvasRoot.instance.trScreenParent, CanvasRoot.instance.trPopupParent, CanvasRoot.instance.trHudParent);

            base.Init();
        }

        // ----- Set -----

        // 해당 씬의 UI 부모 좌표 찾아오기
        public void SetTrUIParent(Transform trScreen, Transform trPopup, Transform trHud)
        {
            if (trScreen == null || trPopup == null || trHud == null)
            {
                MSLog.LogError("UI Parent Transforms cannot be null. Please assign them.");
                return;
            }

            trUIScreen = trScreen;
            trUIPopup = trPopup;
            trUIHud = trHud;

            rectUIScreen = trUIScreen.GetComponent<RectTransform>();
            rectUIPopup = trUIPopup.GetComponent<RectTransform>();
            rectUIHud = trUIHud.GetComponent<RectTransform>();
        }

        public void SetMainCamera() => mainCam = Camera.main;

        public void ResetUIList()
        {
            dicUI.Clear();
        }

        // ----- Get -----

        public Camera GetMainCam() => mainCam;

        public UIBase GetUI(UIType type)
        {
            if (dicUI.ContainsKey(type))
                return dicUI[type];

            return null;
        }

        public T GetUI<T>(UIType type)
        {
            if (dicUI.ContainsKey(type))
                return dicUI[type].GetComponent<T>();

            return default(T);
        }

        private Transform GetParent(Canvas_SortOrder sort)
        {
            switch (sort)
            {
                case Canvas_SortOrder.POPUP:
                    return trUIPopup;
                case Canvas_SortOrder.SCREEN:
                    return trUIScreen;
                case Canvas_SortOrder.HUD:
                    return trUIHud;
                default:
                    return null;
            }
        }

        public RectTransform GetRectUIScreen() => rectUIScreen;
        public RectTransform GetRectUIPopup() => rectUIPopup;
        public RectTransform GetRectUIHud() => rectUIHud;

        // ----- Main -----

        // 비동기 버전 
        public async UniTask OpenUIAsync(UIType uiType, Canvas_SortOrder sortOrder = Canvas_SortOrder.POPUP, Action openAfter = null, Action closeAfter = null, UIAssetSource source = UIAssetSource.Addressables)
        {
            var openUI = GetUI(uiType);
            if (openUI == null)
            {
#if ADDRESSABLES
                openUI = source == UIAssetSource.Addressables
                                    ? await LoadUIAsync_Addressables(uiType, sortOrder)
                                    : await LoadUIAsync_Resources(uiType, sortOrder);
#else
                openUI = await LoadUIAsync_Resources(uiType, sortOrder);
#endif
                if (openUI == null)
                {
                    MSLog.LogError($"Failed to load UI (async): {uiType} via {source}");
                    return;
                }
            }

            WireOpenCloseCallbacks(openUI, openAfter, closeAfter);
            if (!openUIList.Contains(openUI))
                openUIList.Add(openUI);

            openUI.OpenUI();
            RefreshPopupSortingOrder();
        }

        // 동기 버전

        public T OpenUI<T>(UIType uiType, Canvas_SortOrder sortOrder = Canvas_SortOrder.POPUP, Action openAfter = null, Action closeAfter = null, UIAssetSource source = UIAssetSource.Addressables) where T : UIBase
        {
            var openUI = GetUI(uiType);
            if (openUI == null)
            {
#if ADDRESSABLES
                openUI = source == UIAssetSource.Addressables
                                    ? LoadUISync_Addressables(uiType, sortOrder)
                                     : LoadUISync_Resources(uiType, sortOrder);
#else
                openUI = LoadUISync_Resources(uiType, sortOrder);
#endif

                if (openUI == null)
                {
                    MSLog.LogError($"Failed to load UI (sync): {uiType} via {source}");
                    return null;
                }
            }

            WireOpenCloseCallbacks(openUI, openAfter, closeAfter);
            if (!openUIList.Contains(openUI))
                openUIList.Add(openUI);

            openUI.OpenUI();
            RefreshPopupSortingOrder();

            return openUI as T;
        }

        // ----- Logic -----

        private void WireOpenCloseCallbacks(UIBase openUI, Action openAfter, Action closeAfter)
        {
            openUI.callOpenAfter = () =>
            {
                openAfter?.Invoke();
            };

            openUI.callCloseAfter = () =>
            {
                openUIList.Remove(openUI);
                closeAfter?.Invoke();
                RefreshPopupSortingOrder();

                // 닫힐 때 인스턴스 파기 정책이 있다면 여기에서 ReleaseInstance까지 처리
                // if (dicInstHandle.TryGetValue(openUI.UIType, out var handle) && handle.IsValid())
                // {
                //     Addressables.ReleaseInstance(handle);
                //     dicInstHandle.Remove(openUI.UIType);
                //     dicUI.Remove(openUI.UIType);
                // }
            };
        }

#if ADDRESSABLES
        // 비동기 InstantiateAsync → UIBase 캐싱
        private async UniTask<UIBase> LoadUIAsync_Addressables(UIType uiType, Canvas_SortOrder sortOrder = Canvas_SortOrder.POPUP)
        {
            if (trUIScreen == null || trUIPopup == null)
            {
                MSLog.LogError("UI parent transforms are not set. OpenUI failed.");
                return null;
            }

            Transform parentTr = GetParent(sortOrder);
            string key = UIAttrUtil.GetUIAttributeResourceName(uiType); // Addressables 키

            AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(key, parentTr);

            await instHandle.ToUniTask();

            if (instHandle.Status != AsyncOperationStatus.Succeeded || instHandle.Result == null)
            {
                MSLog.LogError($"Addressables instantiate failed: {key}");
                return null;
            }

            var go = instHandle.Result;
            var uiBase = go.GetComponent<UIBase>();
            if (uiBase == null)
            {
                MSLog.LogError($"Instantiated UI has no UIBase: {key}");
                // 생성된 인스턴스 해제
                Addressables.ReleaseInstance(instHandle);
                return null;
            }

            // 캐시
            dicUI[uiType] = uiBase;
            dicInstHandle[uiType] = instHandle;
            return uiBase;
        }

        // 동기 WaitForCompletion 사용
        private UIBase LoadUISync_Addressables(UIType uiType, Canvas_SortOrder sortOrder = Canvas_SortOrder.POPUP)
        {
            if (trUIScreen == null || trUIPopup == null)
            {
                MSLog.LogError("UI parent transforms are not set. OpenUI failed.");
                return null;
            }

            Transform parentTr = GetParent(sortOrder);
            string key = UIAttrUtil.GetUIAttributeResourceName(uiType);

            var instHandle = Addressables.InstantiateAsync(key, parentTr);
            var go = instHandle.WaitForCompletion(); // 동기 대기

            if (go == null || instHandle.Status != AsyncOperationStatus.Succeeded)
            {
                MSLog.LogError($"Addressables instantiate failed (sync): {key}");
                if (instHandle.IsValid())
                    Addressables.ReleaseInstance(instHandle);
                return null;
            }

            var uiBase = go.GetComponent<UIBase>();
            if (uiBase == null)
            {
                MSLog.LogError($"Instantiated UI has no UIBase (sync): {key}");
                Addressables.ReleaseInstance(instHandle);
                return null;
            }

            dicUI[uiType] = uiBase;
            dicInstHandle[uiType] = instHandle;
            return uiBase;
        }
#endif

        // Resources 비동기
        private async UniTask<UIBase> LoadUIAsync_Resources(UIType uiType, Canvas_SortOrder sortOrder)
        {
            if (trUIScreen == null || trUIPopup == null || trUIHud == null)
            {
                MSLog.LogError("UI parents not set. OpenUI failed.");
                return null;
            }

            Transform parentTr = GetParent(sortOrder);
            string path = UIAttrUtil.GetUIAttributeResourceName(uiType);

            // 1) 비동기 로드
            ResourceRequest req = Resources.LoadAsync<GameObject>(path);
            await req.ToUniTask(); // UniTask await (AsyncOperation 확장)

            var prefab = req.asset as GameObject;
            if (prefab == null)
            {
                MSLog.LogError($"Resources.Load failed: {path}");
                return null;
            }

            // 2) 인스턴스화 & 부모 지정
            var go = GameObject.Instantiate(prefab, parentTr, worldPositionStays: false);
            var uiBase = go.GetComponent<UIBase>();
            if (uiBase == null)
            {
                MSLog.LogError($"Instantiated UI has no UIBase (Resources): {path}");
                GameObject.Destroy(go);
                return null;
            }

            // 3) 캐시
            dicUI[uiType] = uiBase;
            return uiBase;
        }

        // Resources 동기
        private UIBase LoadUISync_Resources(UIType uiType, Canvas_SortOrder sortOrder)
        {
            if (trUIScreen == null || trUIPopup == null || trUIHud == null)
            {
                MSLog.LogError("UI parents not set. OpenUI failed.");
                return null;
            }

            Transform parentTr = GetParent(sortOrder);
            string path = UIAttrUtil.GetUIAttributeResourceName(uiType);

            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                MSLog.LogError($"Resources.Load (sync) failed: {path}");
                return null;
            }

            var go = GameObject.Instantiate(prefab, parentTr, worldPositionStays: false);
            var uiBase = go.GetComponent<UIBase>();
            if (uiBase == null)
            {
                MSLog.LogError($"Instantiated UI has no UIBase (Resources sync): {path}");
                GameObject.Destroy(go);
                return null;
            }

            dicUI[uiType] = uiBase;
            return uiBase;
        }

        private void RefreshPopupSortingOrder()
        {
            int count = openUIList.Count;
            int popupIndex = (int)Canvas_SortOrder.POPUP;
            int screenIndex = (int)Canvas_SortOrder.SCREEN;

            for (int i = 0; i < count; i++)
            {
                if (openUIList[i] == null)
                    continue;

                Canvas baseCanvas = openUIList[i].GetCanvas();
                if (baseCanvas != null && openUIList[i].sortOrder == Canvas_SortOrder.POPUP)
                {
                    baseCanvas.sortingOrder = popupIndex;
                    popupIndex += 10;
                }
                else if (baseCanvas != null && openUIList[i].sortOrder == Canvas_SortOrder.SCREEN)
                {
                    baseCanvas.sortingOrder = screenIndex;
                    screenIndex += 10;
                }
            }
        }
    }
}