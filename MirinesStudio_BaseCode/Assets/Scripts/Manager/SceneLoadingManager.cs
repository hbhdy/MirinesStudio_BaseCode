using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MirinesStudio
{
    public class SceneLoadingManager : BaseManager
    {
        // ----- Param -----

        [Range(0.1f, 5f)]
        [SerializeField]
        private float barSpeed = 1.2f;

        private float currentProgress;
        private float targetProgress;
        private bool isLoading;
        private CancellationTokenSource loadingBarCancellation;
        private SceneContext currentSceneContext;

        public bool IsLoading => isLoading;

        public event Action<float> OnProgressChanged;
        public event Action<bool> OnLoadingUISetActive;

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            base.Init();
        }

        public override void Shutdown()
        {
            currentSceneContext?.Shutdown();
            currentSceneContext = null;
            StopTargetSceneSync();
            base.Shutdown();
        }

        // ----- Set -----

        public void SetTargetProgress(float target) => targetProgress = Mathf.Clamp01(target);

        public void StartTargetSceneSync()
        {
            StopTargetSceneSync();
            loadingBarCancellation = new CancellationTokenSource();
            SyncLoadingBarAsync(loadingBarCancellation.Token).Forget();
        }

        public void StopTargetSceneSync()
        {
            if (loadingBarCancellation == null)
                return;

            loadingBarCancellation.Cancel();
            loadingBarCancellation.Dispose();
            loadingBarCancellation = null;
            currentProgress = 1f;
            OnProgressChanged?.Invoke(currentProgress);
            OnLoadingUISetActive?.Invoke(false);
        }

        // ----- Get -----

        // ----- Main -----

        public UniTask LoadInitialSceneAsync(string targetScene, CancellationToken cancellationToken = default) => LoadSingleSceneAsync(targetScene, false, cancellationToken);

        public UniTask SceneLoadingWithAsync(string targetScene, CancellationToken cancellationToken = default) => LoadSingleSceneAsync(targetScene, false, cancellationToken);

        public UniTask SceneLoadingWithAsyncUI(string targetScene, CancellationToken cancellationToken = default) => LoadSingleSceneAsync(targetScene, true, cancellationToken);

        public async UniTask SceneLoadingWithAdditive(string targetScene, bool setActive = true, CancellationToken cancellationToken = default)
        {
            if (isLoading)
                throw new InvalidOperationException("A scene is already loading.");

            isLoading = true;

            try
            {
                GameCore.UI?.GlobalRoot?.SetTransitionActive(true);
                AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
                if (operation == null)
                    throw new InvalidOperationException($"Failed to start loading scene: {targetScene}");

                operation.allowSceneActivation = false;

                while (operation.progress < 0.9f)
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                operation.allowSceneActivation = true;
                await operation.ToUniTask(cancellationToken: cancellationToken);

                if (setActive)
                {
                    Scene scene = SceneManager.GetSceneByName(targetScene);
                    if (scene.IsValid())
                        SceneManager.SetActiveScene(scene);
                }
            }
            finally
            {
                isLoading = false;
                GameCore.UI?.GlobalRoot?.SetTransitionActive(false);
            }
        }

        public async UniTask SceneUnload(string sceneName, CancellationToken cancellationToken = default)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
                return;

            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            await operation.ToUniTask(cancellationToken: cancellationToken);

            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        // ----- Logic -----

        private async UniTask LoadSingleSceneAsync(string targetScene, bool useLoadingUI, CancellationToken cancellationToken)
        {
            if (isLoading)
                throw new InvalidOperationException("A scene is already loading.");

            if (string.IsNullOrWhiteSpace(targetScene))
                throw new ArgumentException("Target scene name is empty.", nameof(targetScene));

            isLoading = true;

            try
            {
                GameCore.UI?.GlobalRoot?.SetTransitionActive(true);
                GameCore.SetState(GameCoreState.LoadingScene);

                if (useLoadingUI)
                {
                    StartTargetSceneSync();
                    SetTargetProgress(0.05f);
                }

                currentSceneContext?.Shutdown();
                currentSceneContext = null;

                AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
                if (operation == null)
                    throw new InvalidOperationException($"Failed to start loading scene: {targetScene}");

                operation.allowSceneActivation = false;

                while (operation.progress < 0.9f)
                {
                    if (useLoadingUI)
                    {
                        float progress = Mathf.Clamp01(operation.progress / 0.9f);
                        SetTargetProgress(Mathf.Lerp(0.05f, 0.70f, progress));
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (useLoadingUI)
                    SetTargetProgress(0.85f);

                operation.allowSceneActivation = true;
                await operation.ToUniTask(cancellationToken: cancellationToken);

                Scene loadedScene = SceneManager.GetSceneByName(targetScene);
                SceneContext sceneContext = FindSceneContext(loadedScene);
                if (sceneContext == null)
                    throw new InvalidOperationException($"SceneContext is missing in scene: {targetScene}");

                if (useLoadingUI)
                    SetTargetProgress(0.95f);

                await sceneContext.InitializeAsync(cancellationToken);
                currentSceneContext = sceneContext;

                GameCore.SetState(GameCoreState.Ready);

                if (useLoadingUI)
                    SetTargetProgress(1f);
            }
            catch (OperationCanceledException)
            {
                if (GameCore.State != GameCoreState.ShuttingDown)
                    GameCore.SetState(GameCoreState.CoreReady);

                throw;
            }
            catch (Exception e)
            {
                GameCore.SetFailure(e);
                throw;
            }
            finally
            {
                isLoading = false;

                if (useLoadingUI)
                    StopTargetSceneSync();
                GameCore.UI?.GlobalRoot?.SetTransitionActive(false);
            }
        }

        private async UniTask SyncLoadingBarAsync(CancellationToken cancellationToken)
        {
            currentProgress = 0f;
            SetTargetProgress(0f);
            OnLoadingUISetActive?.Invoke(true);
            OnProgressChanged?.Invoke(currentProgress);

            try
            {
                while (currentProgress < 0.999f)
                {
                    currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, barSpeed * Time.unscaledDeltaTime);
                    OnProgressChanged?.Invoke(currentProgress);
                    await UniTask.Delay(TimeSpan.FromMilliseconds(10), ignoreTimeScale: true, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static SceneContext FindSceneContext(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                SceneContext context = roots[i].GetComponentInChildren<SceneContext>(true);
                if (context != null)
                    return context;
            }

            return null;
        }
    }
}
