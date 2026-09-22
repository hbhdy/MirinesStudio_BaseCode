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
        float barSpeed = 1.2f; // 초당 진행량
        private float currentProgress = 0.0f;
        private float targetProgress = 0.0f;

        private bool _isLoading;
        private CancellationTokenSource _ctsBar;

        // 외부에서 바인딩할 수 있도록 이벤트 노출 (선택)
        public event Action<float> OnProgressChanged;
        public event Action<bool> OnLoadingUISetActive; // true=표시/false=숨김

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            base.Init();
        }

        // ----- Set -----

        public void SetTargetProgress(float target)
        {
            targetProgress = Mathf.Clamp01(target);
        }

        // 로딩바 동기화 루프 시작
        public void StartTargetSceneSync()
        {
            StopTargetSceneSync(); // 중복 방지
            _ctsBar = new CancellationTokenSource();
            _ = SyncLoadingBarAsync(_ctsBar.Token); // fire-and-forget
        }

        // 로딩바 동기화 루프 중단
        public void StopTargetSceneSync()
        {
            if (_ctsBar != null)
            {
                _ctsBar.Cancel();
                _ctsBar.Dispose();
                _ctsBar = null;
            }
        }

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

        // SetTargetProgress(value)를 통한 로딩바 제어 (로딩상태에서 데이터 처리 및 연출을 위함)
        private async UniTaskVoid SyncLoadingBarAsync(CancellationToken ct)
        {
            currentProgress = 0f;
            SetTargetProgress(0f);

            // 필요 시 UI 표시 신호
            OnLoadingUISetActive?.Invoke(true);
            OnProgressChanged?.Invoke(currentProgress);

            try
            {
                while (currentProgress < 0.999f)
                {
                    currentProgress = Mathf.MoveTowards(
                        currentProgress,
                        targetProgress,
                        barSpeed * Time.unscaledDeltaTime
                    );

                    OnProgressChanged?.Invoke(currentProgress);

                    await UniTask.Delay(TimeSpan.FromMilliseconds(10),
                        ignoreTimeScale: true, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소 처리
            }
            finally
            {
                // 종료 시 보장: 바를 1.0으로 맞추고 UI 끄기
                currentProgress = 1f;
                OnProgressChanged?.Invoke(currentProgress);
                OnLoadingUISetActive?.Invoke(false);
            }
        }

        // 단순 씬 전환
        public async UniTask SceneLoadingWithAsync(string targetScene, CancellationToken ct = default)
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                var async = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
                async.allowSceneActivation = false;

                // 0%→90% (엔진 로딩)
                while (async.progress < 0.9f)
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);

                // 활성화
                async.allowSceneActivation = true;

                // 완료 대기
                await async.ToUniTask(cancellationToken: ct);
            }
            finally
            {
                _isLoading = false;
            }
        }

        // 단순 씬 + 로딩 UI 전환
        public async UniTask SceneLoadingWithAsyncUI(string targetScene, CancellationToken ct = default)
        {
            if (_isLoading) return;
            _isLoading = true;
            StartTargetSceneSync(); // UI on + 바 루프 시작
            try
            {
                // 시작 구간 (0%→5%)
                SetTargetProgress(0.05f);

                var async = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
                async.allowSceneActivation = false;

                // 파일/씬 로딩 (5%→70%)
                while (async.progress < 0.9f)
                {
                    float p = Mathf.Clamp01(async.progress / 0.9f); // 0..1
                    SetTargetProgress(Mathf.Lerp(0.05f, 0.70f, p));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                // 활성화 직전 준비 (70%→85%)
                SetTargetProgress(0.85f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                // 활성화
                async.allowSceneActivation = true;
                await async.ToUniTask(cancellationToken: ct);

                // 씬 내부 시스템 준비 대기 (예: GameCore.isCoreReady) (85%→95%)
                SetTargetProgress(0.95f);
                await UniTask.WaitUntil(() => GameCore.isCoreReady, PlayerLoopTiming.Update, ct);

                // 마무리 (95%→100%)
                SetTargetProgress(1.0f);
                // 로딩바 루프는 1.0에 도달하면 UI를 자동 끕니다.
            }
            finally
            {
                _isLoading = false;
                StopTargetSceneSync(); // 안전 종료 (이미 1.0에서 꺼졌더라도 중복 안전)
            }
        }

        // 단순 씬 추가
        public async UniTask SceneLoadingWithAdditive(string targetScene, bool setActive = true, CancellationToken ct = default)
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                var async = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
                async.allowSceneActivation = false;

                while (async.progress < 0.9f)
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);

                async.allowSceneActivation = true;
                await async.ToUniTask(cancellationToken: ct);

                if (setActive)
                {
                    var scn = SceneManager.GetSceneByName(targetScene);
                    if (scn.IsValid()) SceneManager.SetActiveScene(scn);
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        // 씬 해제
        public async UniTask SceneUnload(string sceneName, CancellationToken ct = default)
        {
            var scn = SceneManager.GetSceneByName(sceneName);
            if (!scn.isLoaded) return;

            var async = SceneManager.UnloadSceneAsync(sceneName);
            await async.ToUniTask(cancellationToken: ct);

            // 메모리 정리 (모바일 권장)
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}
