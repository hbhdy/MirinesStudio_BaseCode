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
        private SceneLoadingManager sceneLoading;
        private int sceneVersion;

        public GlobalCanvasRoot GlobalRoot { get; private set; }

#if ADDRESSABLES
        // Addressables 인스턴스 해제용
        private readonly Dictionary<UIType, AsyncOperationHandle<GameObject>> dicInstHandle = new Dictionary<UIType, AsyncOperationHandle<GameObject>>();
#endif

        private readonly Dictionary<UIType, UIBase> dicUI = new Dictionary<UIType, UIBase>();
        private readonly List<UIBase> openUIList = new List<UIBase>();
        private readonly List<UIPopupBase> popupStack = new List<UIPopupBase>();

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            ClearSceneReferences();
            GlobalRoot = GameCore.GetInstance().GetComponentInChildren<GlobalCanvasRoot>(true);
            if (GlobalRoot == null || GlobalRoot.globalCanvas == null || GlobalRoot.trPopupParent == null)
                throw new InvalidOperationException("GlobalCanvasRoot references are missing in GameCore.");

            GlobalRoot.Init();
            sceneLoading = GameCore.SCENE;
            if (sceneLoading != null)
            {
                sceneLoading.OnLoadingUISetActive += GlobalRoot.SetLoadingActive;
                sceneLoading.OnProgressChanged += GlobalRoot.SetLoadingProgress;
            }

            base.Init();
        }

        public override void Shutdown()
        {
            if (sceneLoading != null && GlobalRoot != null)
            {
                sceneLoading.OnLoadingUISetActive -= GlobalRoot.SetLoadingActive;
                sceneLoading.OnProgressChanged -= GlobalRoot.SetLoadingProgress;
                GlobalRoot.SetLoadingActive(false);
                GlobalRoot.SetTransitionActive(false);
            }
            ResetUIList();
            base.Shutdown();
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

        public bool BindSceneReferences(Camera camera, CanvasRoot root)
        {
            ClearSceneReferences();

            if (camera == null || root == null || root.trScreenParent == null || root.trPopupParent == null || root.trHudParent == null)
                return false;

            mainCam = camera;
            SetTrUIParent(root.trScreenParent, root.trPopupParent, root.trHudParent);
            return true;
        }

        public bool RefreshSceneReferences() => BindSceneReferences(Camera.main, CanvasRoot.instance);

        public void ClearSceneReferences()
        {
            sceneVersion++;
            mainCam = null;
            trUIScreen = null;
            trUIPopup = null;
            trUIHud = null;
            rectUIScreen = null;
            rectUIPopup = null;
            rectUIHud = null;
            var sceneKeys = new List<UIType>();
            foreach (var pair in dicUI)
            {
                if (pair.Value == null || pair.Value.sortOrder != Canvas_SortOrder.GLOBAL)
                    sceneKeys.Add(pair.Key);
            }
            foreach (UIType key in sceneKeys)
                ReleaseUI(key);
            openUIList.RemoveAll(ui => ui == null || ui.sortOrder != Canvas_SortOrder.GLOBAL);
            popupStack.RemoveAll(ui => ui == null || ui.sortOrder != Canvas_SortOrder.GLOBAL);
        }

        public void ResetUIList()
        {
            foreach (UIType key in new List<UIType>(dicUI.Keys))
                ReleaseUI(key);
            openUIList.Clear();
            popupStack.Clear();
        }

        private void ReleaseUI(UIType key)
        {
            UIBase ui = dicUI[key];
            if (ui != null)
            {
                ui.callOpenAfter = null;
                ui.callCloseAfter = null;
            }
#if ADDRESSABLES
            if (dicInstHandle.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                    Addressables.ReleaseInstance(handle);
                dicInstHandle.Remove(key);
            }
            else
#endif
            if (ui != null)
                Destroy(ui.gameObject);
            dicUI.Remove(key);
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
                case Canvas_SortOrder.GLOBAL:
                    return GlobalRoot != null ? GlobalRoot.trPopupParent : null;
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
            if (GetParent(sortOrder) == null)
                return;
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

            openUI.transform.SetParent(GetParent(sortOrder), false);
            openUI.sortOrder = sortOrder;
            WireOpenCloseCallbacks(openUI, openAfter, closeAfter);
            RegisterOpenUI(openUI);

            openUI.OpenUI();
            RefreshPopupSortingOrder();
        }

        // 동기 버전

        public T OpenUI<T>(UIType uiType, Canvas_SortOrder sortOrder = Canvas_SortOrder.POPUP, Action openAfter = null, Action closeAfter = null, UIAssetSource source = UIAssetSource.Addressables) where T : UIBase
        {
            if (GetParent(sortOrder) == null)
                return null;
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

            openUI.transform.SetParent(GetParent(sortOrder), false);
            openUI.sortOrder = sortOrder;
            WireOpenCloseCallbacks(openUI, openAfter, closeAfter);
            RegisterOpenUI(openUI);

            openUI.OpenUI();
            RefreshPopupSortingOrder();

            return openUI as T;
        }

        // ----- Logic -----

        public bool TryHandleBack()
        {
            if (GlobalRoot != null && GlobalRoot.IsTransitioning)
                return true;

            popupStack.RemoveAll(popup => popup == null || !popup.gameObject.activeInHierarchy);
            for (int i = popupStack.Count - 1; i >= 0; i--)
            {
                if (popupStack[i].sortOrder == Canvas_SortOrder.GLOBAL)
                    return popupStack[i].HandleBack();
            }

            while (popupStack.Count > 0)
            {
                int lastIndex = popupStack.Count - 1;
                UIPopupBase popup = popupStack[lastIndex];

                if (popup == null || !popup.gameObject.activeInHierarchy)
                {
                    popupStack.RemoveAt(lastIndex);
                    continue;
                }

                return popup.HandleBack();
            }

            return false;
        }

        private void RegisterOpenUI(UIBase openUI)
        {
            openUIList.Remove(openUI);
            openUIList.Add(openUI);

            if (openUI is not UIPopupBase popup)
                return;

            popupStack.Remove(popup);
            popupStack.Add(popup);
        }

        private void WireOpenCloseCallbacks(UIBase openUI, Action openAfter, Action closeAfter)
        {
            openUI.callOpenAfter = () =>
            {
                openAfter?.Invoke();
            };

            openUI.callCloseAfter = () =>
            {
                openUIList.Remove(openUI);

                if (openUI is UIPopupBase popup)
                    popupStack.Remove(popup);

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
            if (GetParent(sortOrder) == null)
            {
                MSLog.LogError("UI parent transforms are not set. OpenUI failed.");
                return null;
            }

            Transform parentTr = GetParent(sortOrder);
            string key = UIAttrUtil.GetUIAttributeResourceName(uiType); // Addressables 키

            AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(key, parentTr);

            int version = sceneVersion;
            await instHandle.ToUniTask();

            if (parentTr == null || (sortOrder != Canvas_SortOrder.GLOBAL && version != sceneVersion))
            {
                if (instHandle.IsValid())
                    Addressables.ReleaseInstance(instHandle);
                return null;
            }

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
            if (GetParent(sortOrder) == null)
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
            if (GetParent(sortOrder) == null)
            {
                MSLog.LogError("UI parents not set. OpenUI failed.");
                return null;
            }

            Transform parentTr = GetParent(sortOrder);
            string path = UIAttrUtil.GetUIAttributeResourceName(uiType);

            // 1) 비동기 로드
            ResourceRequest req = Resources.LoadAsync<GameObject>(path);
            int version = sceneVersion;
            await req.ToUniTask(); // UniTask await (AsyncOperation 확장)

            if (parentTr == null || (sortOrder != Canvas_SortOrder.GLOBAL && version != sceneVersion))
                return null;

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
            if (GetParent(sortOrder) == null)
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
            int globalIndex = (int)Canvas_SortOrder.GLOBAL;

            for (int i = 0; i < count; i++)
            {
                if (openUIList[i] == null)
                    continue;

                Canvas baseCanvas = openUIList[i].GetCanvas();
                if (baseCanvas != null)
                {
                    baseCanvas.overrideSorting = true;
                    Canvas parentCanvas = openUIList[i].transform.parent.GetComponentInParent<Canvas>();
                    if (parentCanvas != null)
                        baseCanvas.sortingLayerID = parentCanvas.sortingLayerID;
                }
                if (baseCanvas != null && openUIList[i].sortOrder == Canvas_SortOrder.GLOBAL)
                {
                    baseCanvas.sortingOrder = globalIndex;
                    globalIndex += 10;
                }
                else if (baseCanvas != null && openUIList[i].sortOrder == Canvas_SortOrder.POPUP)
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
