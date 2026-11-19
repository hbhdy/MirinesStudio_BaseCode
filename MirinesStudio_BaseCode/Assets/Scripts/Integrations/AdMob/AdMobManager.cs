#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System;
using System.Threading;
using UnityEngine;

namespace MirinesStudio
{
    public sealed class AdMobManager : IDisposable
    {
#if Live && !Dev
        private const string adUnitId_Banner = "";
        private const string adUnitId_Interstitial = "";
        private const string adUnitId_Rewarded = "";
#elif UNITY_IOS
        private const string adUnitId_Banner = "ca-app-pub-3940256099942544/2934735716";
        private const string adUnitId_Interstitial = "ca-app-pub-3940256099942544/4411468910";
        private const string adUnitId_Rewarded = "ca-app-pub-3940256099942544/1712485313";
#else
        private const string adUnitId_Banner = "ca-app-pub-3940256099942544/6300978111";
        private const string adUnitId_Interstitial = "ca-app-pub-3940256099942544/1033173712";
        private const string adUnitId_Rewarded = "ca-app-pub-3940256099942544/5224354917";
#endif

        private const int InterstitialInterval = 2;
        private const int MaxLoadRetries = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private BannerView bannerAd;
        private InterstitialAd interstitialAd;
        private RewardedAd rewardedAd;
        private UniTaskCompletionSource<AdOutcome> interstitialRequest;
        private UniTaskCompletionSource<AdOutcome> rewardedRequest;
        private bool interstitialLoading;
        private bool rewardedLoading;
        private bool interstitialRetryPending;
        private bool rewardedRetryPending;
        private int interstitialRetries;
        private int rewardedRetries;
        private int interstitialCount;
        private bool bannerVisible = true;
        private bool initializationStarted;
        private bool disposed;

        public bool IsReady { get; private set; }
        public bool IsRewardedReady => IsReady && !IsShowingFullscreen && rewardedAd != null && rewardedAd.CanShowAd();
        public bool IsInterstitialReady => IsReady && !IsShowingFullscreen && interstitialAd != null && interstitialAd.CanShowAd();

        private bool IsShowingFullscreen => rewardedRequest != null || interstitialRequest != null;

        internal AdMobManager() { }

        internal void Initialize()
        {
            if (string.IsNullOrEmpty(adUnitId_Banner) && string.IsNullOrEmpty(adUnitId_Interstitial) && string.IsNullOrEmpty(adUnitId_Rewarded))
            {
                Debug.LogWarning("[AdMob] Set the ad unit IDs for the Live configuration.");
                return;
            }

            MSLog.Log("[AdMob] Initialization started.");
            try
            {
#if UNITY_EDITOR
                StartSdkInitialization();
#else
                ConsentInformation.Update(new ConsentRequestParameters(), error => MainThreadDispatcher.Post(() => OnConsentInfoUpdated(error)));
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdMob] Startup failed: {e.Message}");
            }
        }

        private void OnConsentInfoUpdated(FormError error)
        {
            if (disposed)
                return;

            if (error != null)
            {
                OnConsentFormFinished(error);
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError => MainThreadDispatcher.Post(() => OnConsentFormFinished(formError)));
        }

        private void OnConsentFormFinished(FormError error)
        {
            if (disposed)
                return;
            if (error != null)
                Debug.LogWarning($"[AdMob] Consent flow failed: {error}");

            if (ConsentInformation.CanRequestAds())
                StartSdkInitialization();
        }

        private void StartSdkInitialization()
        {
            if (disposed || initializationStarted)
                return;

            initializationStarted = true;
            MobileAds.Initialize(status => MainThreadDispatcher.Post(() =>
            {
                if (disposed)
                    return;
                if (status == null)
                {
                    Debug.LogWarning("[AdMob] SDK initialization returned no status.");
                    return;
                }

                GameConfig.Get(GameConfig.ADS_INTERSTITIAL, ref interstitialCount);
                IsReady = true;
                MSLog.Log("[AdMob] SDK initialization complete.");
                //LoadAd_Banner();
                LoadAd_Interstital();
                LoadAd_Rewarded();
            }));
        }

        public bool ShouldShowInterstitialThisTime()
        {
            if (!IsReady)
                return false;

            bool shouldShow = ++interstitialCount >= InterstitialInterval;
            if (shouldShow)
                interstitialCount = 0;
            GameConfig.Set(GameConfig.ADS_INTERSTITIAL, interstitialCount);
            return shouldShow;
        }

        // ----- Banner -----

        public void CreateBannerView()
        {
            if (!IsReady || string.IsNullOrWhiteSpace(adUnitId_Banner))
                return;

            DestroyAd_Banner();
            var ad = new BannerView(adUnitId_Banner, AdSize.Banner, AdPosition.Bottom);
            bannerAd = ad;
            ad.OnBannerAdLoaded += () => MainThreadDispatcher.Post(() =>
            {
                if (bannerAd != ad)
                    return;
                if (bannerVisible)
                    ad.Show();
                else
                    ad.Hide();
            });
            ad.OnBannerAdLoadFailed += error => MainThreadDispatcher.Post(() => MSLog.LogError($"[AdMob] Banner load failed: {error}"));
        }

        public void LoadAd_Banner()
        {
            if (!IsReady)
                return;
            if (bannerAd == null)
                CreateBannerView();
            bannerAd?.LoadAd(new AdRequest());
        }

        public void ShowAd_Banner()
        {
            bannerVisible = true;
            if (bannerAd == null)
                LoadAd_Banner();
            else
                bannerAd.Show();
        }

        public void HideAd_Banner()
        {
            bannerVisible = false;
            bannerAd?.Hide();
        }

        public void DestroyAd_Banner()
        {
            bannerAd?.Destroy();
            bannerAd = null;
        }

        public void LogResponseInfo_Banner()
        {
            if (bannerAd != null)
                MSLog.Log($"[AdMob] Banner response: {bannerAd.GetResponseInfo()}");
        }

        // ----- Interstitial -----

        public void LoadAd_Interstital()
        {
            if (!IsReady || interstitialLoading || interstitialRetryPending || interstitialAd != null || string.IsNullOrWhiteSpace(adUnitId_Interstitial))
                return;

            interstitialLoading = true;
            InterstitialAd.Load(adUnitId_Interstitial, new AdRequest(), (ad, error) => MainThreadDispatcher.Post(() =>
            {
                interstitialLoading = false;
                if (disposed || error != null || ad == null)
                {
                    ad?.Destroy();
                    if (!disposed)
                    {
                        MSLog.LogError($"[AdMob] Interstitial load failed: {error}");
                        RetryLoadAsync(false).Forget();
                    }
                    return;
                }

                interstitialAd = ad;
                interstitialRetries = 0;
            }));
        }

        public void ShowAd_Interstital() => ShowInterstitialAsync().Forget();

        public UniTask<AdOutcome> ShowInterstitialAsync(CancellationToken ct = default)
        {
            if (!IsReady || IsShowingFullscreen || ct.IsCancellationRequested)
                return UniTask.FromResult(ct.IsCancellationRequested ? AdOutcome.Failed : AdOutcome.NotAvailable);
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return UniTask.FromResult(AdOutcome.NotNetwork);
            if (!IsInterstitialReady)
            {
                if (!interstitialRetryPending && !interstitialLoading)
                {
                    interstitialAd?.Destroy();
                    interstitialAd = null;
                    interstitialRetries = 0;
                    LoadAd_Interstital();
                }
                return UniTask.FromResult(AdOutcome.NotAvailable);
            }

            var request = new UniTaskCompletionSource<AdOutcome>();
            interstitialRequest = request;
            interstitialAd.OnAdFullScreenContentOpened += () => MainThreadDispatcher.Post(() => OnFullscreenOpened(request));
            interstitialAd.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Post(() => FinishInterstitial(request, AdOutcome.Completed));
            interstitialAd.OnAdFullScreenContentFailed += error => MainThreadDispatcher.Post(() =>
            {
                MSLog.LogError($"[AdMob] Interstitial show failed: {error}");
                FinishInterstitial(request, AdOutcome.Failed);
            });

            try { interstitialAd.Show(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdMob] Interstitial show failed: {e.Message}");
                FinishInterstitial(request, AdOutcome.Failed);
            }
            return WaitForShowAsync(request, ct);
        }

        private void FinishInterstitial(UniTaskCompletionSource<AdOutcome> request, AdOutcome outcome)
        {
            if (interstitialRequest != request)
                return;

            interstitialRequest = null;
            GameCore.SOUND?.OnAdClosed();
            interstitialAd?.Destroy();
            interstitialAd = null;
            request.TrySetResult(outcome);
            LoadAd_Interstital();
        }

        // ----- Rewarded -----

        public void LoadAd_Rewarded()
        {
            if (!IsReady || rewardedLoading || rewardedRetryPending || rewardedAd != null || string.IsNullOrWhiteSpace(adUnitId_Rewarded))
                return;

            rewardedLoading = true;
            RewardedAd.Load(adUnitId_Rewarded, new AdRequest(), (ad, error) => MainThreadDispatcher.Post(() =>
            {
                rewardedLoading = false;
                if (disposed || error != null || ad == null)
                {
                    ad?.Destroy();
                    if (!disposed)
                    {
                        MSLog.LogError($"[AdMob] Rewarded load failed: {error}");
                        RetryLoadAsync(true).Forget();
                    }
                    return;
                }

                rewardedAd = ad;
                rewardedRetries = 0;
            }));
        }

        public void ShowAd_Rewarded(Action rewardCall) => ShowRewardedAsync(rewardCall).Forget();

        public UniTask<AdOutcome> ShowRewardedAsync(Action rewardCall, CancellationToken ct = default)
        {
            if (!IsReady || IsShowingFullscreen || ct.IsCancellationRequested)
                return UniTask.FromResult(ct.IsCancellationRequested ? AdOutcome.Failed : AdOutcome.NotAvailable);
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return UniTask.FromResult(AdOutcome.NotNetwork);
            if (!IsRewardedReady)
            {
                if (!rewardedRetryPending && !rewardedLoading)
                {
                    rewardedAd?.Destroy();
                    rewardedAd = null;
                    rewardedRetries = 0;
                    LoadAd_Rewarded();
                }
                return UniTask.FromResult(AdOutcome.NotAvailable);
            }

            var request = new UniTaskCompletionSource<AdOutcome>();
            rewardedRequest = request;
            bool earned = false;
            rewardedAd.OnAdFullScreenContentOpened += () => MainThreadDispatcher.Post(() => OnFullscreenOpened(request));
            rewardedAd.OnAdFullScreenContentClosed += () => MainThreadDispatcher.Post(() => FinishRewardedAfterFocusAsync(request, () => earned, rewardCall).Forget());
            rewardedAd.OnAdFullScreenContentFailed += error => MainThreadDispatcher.Post(() =>
            {
                MSLog.LogError($"[AdMob] Rewarded show failed: {error}");
                FinishRewarded(request, AdOutcome.Failed, null);
            });

            try { rewardedAd.Show(_ => MainThreadDispatcher.Post(() => earned = true)); }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdMob] Rewarded show failed: {e.Message}");
                FinishRewarded(request, AdOutcome.Failed, null);
            }
            return WaitForShowAsync(request, ct);
        }

        private async UniTask FinishRewardedAfterFocusAsync(UniTaskCompletionSource<AdOutcome> request, Func<bool> hasReward, Action rewardCall)
        {
            while (rewardedRequest == request && !Application.isFocused)
                await UniTask.NextFrame();

            await UniTask.NextFrame();
            if (rewardedRequest == request)
                FinishRewarded(request, hasReward() ? AdOutcome.Completed : AdOutcome.Skipped, rewardCall);
        }

        private void FinishRewarded(UniTaskCompletionSource<AdOutcome> request, AdOutcome outcome, Action rewardCall)
        {
            if (rewardedRequest != request)
                return;

            rewardedRequest = null;
            GameCore.SOUND?.OnAdClosed();
            rewardedAd?.Destroy();
            rewardedAd = null;
            if (outcome == AdOutcome.Completed)
            {
                try { rewardCall?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            request.TrySetResult(outcome);
            LoadAd_Rewarded();
        }

        // ----- Fullscreen lifecycle -----

        private void OnFullscreenOpened(UniTaskCompletionSource<AdOutcome> request)
        {
            if (rewardedRequest == request || interstitialRequest == request)
                GameCore.SOUND?.OnAdShown();
        }

        private static async UniTask<AdOutcome> WaitForShowAsync(UniTaskCompletionSource<AdOutcome> request, CancellationToken ct)
        {
            try { return await request.Task.AttachExternalCancellation(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return AdOutcome.Failed; }
        }

        private async UniTask RetryLoadAsync(bool rewarded)
        {
            if (rewarded)
            {
                if (rewardedRetryPending || rewardedRetries >= MaxLoadRetries)
                    return;
                rewardedRetryPending = true;
                rewardedRetries++;
            }
            else
            {
                if (interstitialRetryPending || interstitialRetries >= MaxLoadRetries)
                    return;
                interstitialRetryPending = true;
                interstitialRetries++;
            }

            try { await UniTask.Delay(RetryDelay, ignoreTimeScale: true, cancellationToken: lifetime.Token); }
            catch (OperationCanceledException) when (disposed) { return; }

            if (rewarded)
            {
                rewardedRetryPending = false;
                LoadAd_Rewarded();
            }
            else
            {
                interstitialRetryPending = false;
                LoadAd_Interstital();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            IsReady = false;
            lifetime.Cancel();
            lifetime.Dispose();
            var activeReward = rewardedRequest;
            var activeInterstitial = interstitialRequest;
            rewardedRequest = null;
            interstitialRequest = null;

            if (activeReward != null || activeInterstitial != null)
                GameCore.SOUND?.OnAdClosed();

            DestroyAd_Banner();
            interstitialAd?.Destroy();
            interstitialAd = null;
            rewardedAd?.Destroy();
            rewardedAd = null;
            activeReward?.TrySetResult(AdOutcome.Failed);
            activeInterstitial?.TrySetResult(AdOutcome.Failed);
        }
    }
}
#endif
