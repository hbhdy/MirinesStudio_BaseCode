using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MirinesStudio
{
    public enum GameCoreState
    {
        None,
        Initializing,
        CoreReady,
        LoadingScene,
        Ready,
        Failed,
        ShuttingDown,
    }

    public class GameCore : SingletonMono<GameCore>
    {
        // ----- Param -----

        [Header("Base Managers")]
        public List<BaseManager> managerList;

        [Header("First Scene")]
        [SerializeField]
        private string firstSceneName = "Lobby";

        private Dictionary<Type, BaseManager> dicManagers = new Dictionary<Type, BaseManager>();

        public static ResourceManager RSS => Instance?.Get<ResourceManager>();

        public static DataManager DATA => Instance?.Get<DataManager>();

        public static UIManager UI => Instance?.Get<UIManager>();

        public static SoundManager SOUND => Instance?.Get<SoundManager>();
        
        public static LocalizationManager LOCAL => Instance?.Get<LocalizationManager>();

        public static SceneLoadingManager SCENE => Instance?.Get<SceneLoadingManager>();

        //public static FirebaseManager FB => Instance?.Get<FirebaseManager>();

        //public static GPGSManager GPGS => Instance?.Get<GPGSManager>();

        //public static AdMobManager ADMOB => Instance?.Get<AdMobManager>();

        //public static IAPManager IAP => Instance?.Get<IAPManager>();

        public static GameCoreState State { get; private set; } = GameCoreState.None;
        public static Exception InitializationException { get; private set; }

        public static event Action<GameCoreState> OnStateChanged;

        public static bool GetIsCoreReady() => State == GameCoreState.CoreReady || State == GameCoreState.LoadingScene || State == GameCoreState.Ready;
        public static bool GetIsSessionReady() => State == GameCoreState.Ready;
        public static bool GetIsAllReady() => State == GameCoreState.Ready;

        // ----- Init -----

        public T Get<T>() where T : BaseManager
        {
            var type = typeof(T);
            return dicManagers.ContainsKey(type) ? dicManagers[type] as T : null;
        }

        private void Start()
        {
#if UNITY_EDITOR && SHOW_LOG
            if (GetComponent<DevelopmentDebugPanel>() == null)
                gameObject.AddComponent<DevelopmentDebugPanel>();
#endif
            InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            // 프로젝트 기본 내부 설정 처리
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Application.targetFrameRate = 60;
            Time.timeScale = 1;

            InitializationException = null;
            SetState(GameCoreState.Initializing);

            try
            {
                RegisterManagers();
                await InitializeManagersAsync(cancellationToken);

                StaticGameData.Init();
                ActivateManagers();

                SetState(GameCoreState.CoreReady);

                if (SCENE == null)
                    throw new InvalidOperationException("SceneLoadingManager is not registered.");

                await SCENE.LoadInitialSceneAsync(firstSceneName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (State != GameCoreState.ShuttingDown)
                    SetState(GameCoreState.None);
            }
            catch (Exception e)
            {
                SetFailure(e);
                MSLog.LogError($"GameCore initialization failed.\n{e}");
            }
        }

        private void RegisterManagers()
        {
            if (managerList == null || managerList.Count == 0)
                throw new InvalidOperationException("Manager list is empty.");

            dicManagers.Clear();

            for (int i = 0; i < managerList.Count; i++)
            {
                BaseManager manager = managerList[i];
                if (manager == null)
                    throw new InvalidOperationException($"Manager at index {i} is null.");

                Type type = manager.GetType();
                if (!dicManagers.TryAdd(type, manager))
                    throw new InvalidOperationException($"Manager is duplicated: {type.Name}");
            }
        }

        private async UniTask InitializeManagersAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < managerList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await managerList[i].Task_Init();
            }
        }

        private void ActivateManagers()
        {
            for (int i = 0; i < managerList.Count; i++)
                managerList[i].Init();
        }

        private void ShutdownManagers()
        {
            if (managerList == null)
                return;

            for (int i = managerList.Count - 1; i >= 0; i--)
                managerList[i]?.Shutdown();
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        public void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                UI?.TryHandleBack();

            // 임시 팝업 오픈
            if (Keyboard.current[Key.Q].wasPressedThisFrame)
                UI?.OpenUI<PopupUI_Common>(UIType.PopupUI_Common, Canvas_SortOrder.POPUP);

        }

        public void OnApplicationPause(bool pause)
        {

        }

        protected override void OnApplicationQuit()
        {
            SetState(GameCoreState.ShuttingDown);
            ShutdownManagers();
            base.OnApplicationQuit();
        }

        // ----- Logic -----

        public static async UniTask WaitUntilReady(CancellationToken cancellationToken = default)
        {
            await UniTask.WaitUntil(() => State == GameCoreState.Ready || State == GameCoreState.Failed, cancellationToken: cancellationToken);

            if (State == GameCoreState.Failed)
                throw new InvalidOperationException("GameCore initialization failed.", InitializationException);
        }

        internal static void SetState(GameCoreState state)
        {
            if (State == state)
                return;

            State = state;
            OnStateChanged?.Invoke(State);
        }

        internal static void SetFailure(Exception exception)
        {
            InitializationException = exception;
            SetState(GameCoreState.Failed);
        }

    }
}
