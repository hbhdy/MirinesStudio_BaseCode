using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MirinesStudio
{
    public class AdMobAdapter : BaseManager, IAdService
    {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
        private AdMobManager manager;
        public AdMobManager Manager => IsReady ? manager : null;
        public bool IsReady => manager?.IsReady == true;
        public bool IsRewardedReady => manager?.IsRewardedReady == true;
        public bool IsInterstitialReady => manager?.IsInterstitialReady == true;
#else
        public bool IsReady => false;
        public bool IsRewardedReady => false;
        public bool IsInterstitialReady => false;
#endif

        public override UniTask Task_Init() => UniTask.CompletedTask;

        public override void Init()
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            if (manager != null)
                return;
            manager = new AdMobManager();
            manager.Initialize();
#endif
        }

        public bool ShouldShowInterstitialThisTime()
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            return manager?.ShouldShowInterstitialThisTime() == true;
#else
            return false;
#endif
        }

        public UniTask<AdOutcome> ShowRewardedAsync(Action rewardCall, CancellationToken ct = default)
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            if (manager != null)
                return manager.ShowRewardedAsync(rewardCall, ct);
#endif
            return UniTask.FromResult(AdOutcome.NotAvailable);
        }

        public UniTask<AdOutcome> ShowInterstitialAsync(CancellationToken ct = default)
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            if (manager != null)
                return manager.ShowInterstitialAsync(ct);
#endif
            return UniTask.FromResult(AdOutcome.NotAvailable);
        }

        public void ShowAd_Banner()
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            manager?.ShowAd_Banner();
#endif
        }

        public void HideAd_Banner()
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            manager?.HideAd_Banner();
#endif
        }

        public override void Shutdown()
        {
#if MIRINES_ADMOB && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            manager?.Dispose();
            manager = null;
#endif
            base.Shutdown();
        }

        private void OnDestroy() => Shutdown();
    }
}
