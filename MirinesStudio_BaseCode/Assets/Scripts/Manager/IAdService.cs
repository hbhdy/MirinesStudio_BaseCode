using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MirinesStudio
{
    public interface IAdService
    {
        bool IsReady { get; }
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }
        bool ShouldShowInterstitialThisTime();
        UniTask<AdOutcome> ShowRewardedAsync(Action rewardCall, CancellationToken ct = default);
        UniTask<AdOutcome> ShowInterstitialAsync(CancellationToken ct = default);
        void ShowAd_Banner();
        void HideAd_Banner();
    }
}
