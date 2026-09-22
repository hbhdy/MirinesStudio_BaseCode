//using Cysharp.Threading.Tasks;
//using GoogleMobileAds.Api;
//using GoogleMobileAds.Ump.Api;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;

//#if UNITY_IOS
//// IOS 14 패키지 매니저 다운로드 필요
//using Unity.Advertisement.IosSupport; // Unity iOS ATT 바인딩 (UnityPackage에 포함되어 있음)
//#endif

//namespace MirinesStudio
//{
//    public class AdMobManager : BaseManager
//    {
//#if UNITY_ANDROID

//#if Dev
//        // 광고 테스트
//        private string adUnitId_Banner = "ca-app-pub-3940256099942544/6300978111"; 
//        private string adUnitId_Interstitial = "ca-app-pub-3940256099942544/1033173712";
//        private string adUnitId_Rewarded = "ca-app-pub-3940256099942544/5224354917";
//#elif Live
//        // 광고 리얼
//        private string adUnitId_Banner = "";
//        private string adUnitId_Interstitial = "";
//        private string adUnitId_Rewarded = "";
//#else
//        private string adUnitId_Banner = "ca-app-pub-3940256099942544/6300978111";
//        private string adUnitId_Interstitial = "ca-app-pub-3940256099942544/1033173712";
//        private string adUnitId_Rewarded = "ca-app-pub-3940256099942544/5224354917";
//#endif

//#elif UNITY_IOS

//#if Dev
//        // 광고 테스트
//        private string adUnitId_Banner = "ca-app-pub-3940256099942544/2934735716";  
//        private string adUnitId_Interstitial = "ca-app-pub-3940256099942544/4411468910";
//        private string adUnitId_Rewarded = "ca-app-pub-3940256099942544/1712485313";  
//#elif Live
//        // 광고 리얼
//        private string adUnitId_Banner = "";  
//        private string adUnitId_Interstitial = "";
//        private string adUnitId_Rewarded = "";  
//#else
//        private string adUnitId_Banner = "ca-app-pub-3940256099942544/2934735716";  
//        private string adUnitId_Interstitial = "ca-app-pub-3940256099942544/4411468910";
//        private string adUnitId_Rewarded = "ca-app-pub-3940256099942544/1712485313";
//#endif

//#endif

//        private bool _rewardedReady;
//        private bool _interstitialReady;
//        public bool IsRewardedReady => _rewardedReady;
//        public bool IsInterstitialReady => _interstitialReady;

//        // 빈도/쿨다운 간단 유틸
//        private int _NowInterstitialCount = 0;
//        private int _MaxInterstitialCount = 2;

//        private bool _IsInitGDPR = false;
//        private bool _ISInitIDFA = false;

//        private Coroutine _rewardedRetryCo_1;
//        private bool _isRewardedLoading_1;   // LoadAd_Rewarded() 요청 중
//        private bool _isRewardedRetrying_1;  // 재시도 루프 동작중
//        private int _rewardedRetryCount_1;
//        private const int MaxRewardedRetryCount_1 = 3;
//        private int _baseRewardedDelay_1 = 60;

//        private Coroutine _interstitialRetryCo;
//        private bool _isInterstitialLoading;   // LoadAd_Interstital() 요청 중
//        private bool _isInterstitialRetrying;  // 재시도 루프 동작중
//        private int _interstitialRetryCount;
//        private const int MaxInterstitialRetryCount = 3;
//        private int _baseInterstitialDelay = 60;

//        // ----- Init -----

//        public override async UniTask Task_Init()
//        {
//            _IsInitGDPR = false;

//#if !UNITY_EDITOR
//            MSLog.Log("[GDPR] Process Start");
//#if Dev
//            MSLog.Log("[ADMOB] Dev Version Check");

//            ConsentRequestParameters request = new ConsentRequestParameters();
//            ConsentInformation.Update(request, OnConsentInfoUpdated);
//#else
//            MSLog.Log("[ADMOB] Live Version Check");
//            ConsentRequestParameters request = new ConsentRequestParameters();
//            ConsentInformation.Update(request, OnConsentInfoUpdated);
//#endif

//#else
//            MobileAds.Initialize((InitializationStatus initStatus) =>
//            {
//                LoadAd_Banner();
//                LoadAd_Interstital();
//                LoadAd_Rewarded();

//                GameConfig.Get(GameConfig.ADS_INTERSTITIAL, ref _NowInterstitialCount);
//            });

//            _IsInitGDPR = true;
//#endif

//            await UniTask.WaitUntil(() => _IsInitGDPR);

//#if !UNITY_EDITOR
//            RequestAuthorizationTracking();
//#else
//            _ISInitIDFA = true;
//#endif

//            await UniTask.WaitUntil(() => _ISInitIDFA);

//            await base.Task_Init();
//        }

//        public override void Init()
//        {
//            base.Init();
//        }

//        // ----- Set -----

//        // ----- Get -----

//        // ----- Main -----

//        // ----- Logic -----

//        private bool _wasRewardedThisShow;
//        private UniTaskCompletionSource<AdOutcome> _rewardTcs;

//        public UniTask<AdOutcome> ShowRewardedAsync(Action rewardCall, CancellationToken ct = default)
//        {
//            if (Application.internetReachability == NetworkReachability.NotReachable)
//            {
//                // 인터넷 연결 안됨
//                MSLog.Log("[ADMOB] Rewarded No internet connection");
//                return UniTask.FromResult(AdOutcome.NotNetwork);
//            }

//            if (!_rewardedReady || rewardedAd == null || !rewardedAd.CanShowAd())
//            {
//                // 준비 안 되어 있으면 여기서 로드 한번 더 시도
//                MSLog.Log("[ADMOB] Rewarded not ready, start loading");

//                // 재시도 루프가 돌고 있지 않으면 시작
//                if (_isRewardedRetrying_1 == false)
//                {
//                    if (_rewardedRetryCount_1 >= MaxRewardedRetryCount_1)
//                        _rewardedRetryCount_1 = 0;

//                    LoadAd_Rewarded();
//                }

//                return UniTask.FromResult(AdOutcome.NotAvailable);
//            }

//            _rewardTcs = new UniTaskCompletionSource<AdOutcome>();
//            _wasRewardedThisShow = false;
//            _rewardedReady = false; // 지금부터는 준비 상태 아님

//            if (ct.CanBeCanceled)
//                ct.Register(() => _rewardTcs.TrySetResult(AdOutcome.Failed));

//            ShowAd_Rewarded(rewardCall);

//            return _rewardTcs.Task;
//        }

//        private UniTaskCompletionSource<AdOutcome> _interTcs;
//        public UniTask<AdOutcome> ShowInterstitialAsync(CancellationToken ct = default)
//        {
//            if (Application.internetReachability == NetworkReachability.NotReachable)
//            {
//                // 인터넷 연결 안됨
//                MSLog.Log("[ADMOB] Interstitial No internet connection");
//                //return UniTask.FromResult(AdOutcome.NotNetwork);
//            }

//            if (!_interstitialReady || interstitialAd == null || !interstitialAd.CanShowAd())
//            {
//                // 준비 안 되어 있으면 여기서 로드 한번 더 시도
//                MSLog.Log("[ADMOB] Interstitial not ready, start loading");

//                // 재시도 루프가 돌고 있지 않으면 시작
//                if (_isInterstitialRetrying == false)
//                {
//                    if (_interstitialRetryCount >= MaxInterstitialRetryCount)
//                        _interstitialRetryCount = 0;

//                    LoadAd_Interstital();
//                }

//                return UniTask.FromResult(AdOutcome.NotAvailable);
//            }

//            _interTcs = new UniTaskCompletionSource<AdOutcome>();
//            _interstitialReady = false;

//            if (ct.CanBeCanceled)
//                ct.Register(() => _interTcs.TrySetResult(AdOutcome.Failed));

//            ShowAd_Interstital();
//            return _interTcs.Task;
//        }

//        public bool ShouldShowInterstitialThisTime()
//        {
//            _NowInterstitialCount++;
//            if (_NowInterstitialCount >= _MaxInterstitialCount)
//            {
//                _NowInterstitialCount = 0;
//                GameConfig.Set(GameConfig.ADS_INTERSTITIAL, _NowInterstitialCount);
//                return true;
//            }
//            else
//                GameConfig.Set(GameConfig.ADS_INTERSTITIAL, _NowInterstitialCount);
//            return false;
//        }

//        #region Banner_AD

//        private BannerView bannerAD;

//        /// <summary>
//        /// Creates a 320x50 banner at top of the screen
//        /// </summary>
//        public void CreateBannerView()
//        {
//            MSLog.Log("Creating banner view");

//            // If we already have a banner, destroy the old one
//            if (bannerAD != null)
//            {
//                DestroyAd_Banner();
//            }

//            // Create a 320x50 banner at top of the screen
//            bannerAD = new BannerView(adUnitId_Banner, AdSize.Banner, AdPosition.Bottom);

//            // Listen to events the banner may raise.
//            RegisterEventHandlers(bannerAD);

//            MSLog.Log("Banner view created");
//        }

//        public void LoadAd_Banner()
//        {
//            // Create an instance of a banner view first
//            if (bannerAD == null)
//            {
//                CreateBannerView();
//            }

//            // Create our request used to load the ad
//            var adRequest = new AdRequest();

//            // Send the request to load the ad.
//            MSLog.Log("Loading banner ad");
//            bannerAD.LoadAd(adRequest);
//        }

//        public void ShowAd_Banner()
//        {
//            if (bannerAD != null)
//            {
//                MSLog.Log("Showing banner view");
//                bannerAD.Show();
//            }
//        }

//        public void HideAd_Banner()
//        {
//            if (bannerAD != null)
//            {
//                MSLog.Log("Hiding banner view");
//                bannerAD.Hide();
//            }
//        }

//        public void DestroyAd_Banner()
//        {
//            if (bannerAD != null)
//            {
//                MSLog.Log("Destroying banner view");
//                bannerAD.Destroy();
//                bannerAD = null;
//            }
//        }

//        /// <summary>
//        /// Logs the ResponseInfo.
//        /// </summary>
//        public void LogResponseInfo_Banner()
//        {
//            if (bannerAD != null)
//            {
//                var responseInfo = bannerAD.GetResponseInfo();
//                if (responseInfo != null)
//                {
//                    //Debug.Log(responseInfo);
//                }
//            }
//        }

//        private void RegisterEventHandlers(BannerView ad)
//        {
//            // Raised when an ad is loaded into the banner view.
//            ad.OnBannerAdLoaded += () =>
//            {
//                MSLog.Log("Banner view loaded an ad with response : " + ad.GetResponseInfo());
//            };
//            // Raised when an ad fails to load into the banner view
//            ad.OnBannerAdLoadFailed += (LoadAdError error) =>
//            {
//                MSLog.LogError("Banner view failed to load an ad with error : " + error);
//            };
//            // Raised when the ad is estimated to have earned money
//            ad.OnAdPaid += (AdValue adValue) =>
//            {
//                MSLog.Log(String.Format("Banner view paid {0} {1}", adValue.Value, adValue.CurrencyCode));
//            };
//            // Raised when an impression is recorded for an ad
//            ad.OnAdImpressionRecorded += () =>
//            {
//                MSLog.Log("Banner view recorded an impression");
//            };
//            // Raised when a click is recorded for an ad
//            ad.OnAdClicked += () =>
//            {
//                MSLog.Log("Banner view was clicked");
//            };
//            // Raised when an ad opened full screen content
//            ad.OnAdFullScreenContentOpened += () =>
//            {
//                MSLog.Log("Banner view full screen content opened");
//            };
//            // Raised when the ad closed full screen content
//            ad.OnAdFullScreenContentClosed += () =>
//            {
//                MSLog.Log("Banner view full screen content closed");
//            };
//        }

//        #endregion Banner_AD

//        #region Interstitial_AD

//        private InterstitialAd interstitialAd;

//        public void LoadAd_Interstital()
//        {
//            interstitialAd?.Destroy();
//            interstitialAd = null;
//            _interstitialReady = false;

//            var adRequest = new AdRequest();
//            InterstitialAd.Load(adUnitId_Interstitial, adRequest, (InterstitialAd ad, LoadAdError error) =>
//            {
//                _isInterstitialLoading = false;

//                if (error != null || ad == null)
//                {
//                    MSLog.LogError($"[ADMOB] Interstitial load failed: {error}");
//                    _interstitialReady = false;

//                    StartInterstitialRetryLoop(); //  실패 시 재시도 루프
//                    return;
//                }

//                interstitialAd = ad;
//                _interstitialReady = true;
//                _interstitialRetryCount = 0;     //  성공 시 리셋

//                RegisterEventHandlers(ad);
//                MSLog.Log($"[ADMOB] Interstitial loaded: {ad.GetResponseInfo()}");

//                StopInterstitialRetryLoop();     //  성공하면 루프 종료
//            });
//        }

//        public void ShowAd_Interstital()
//        {
//            if (interstitialAd != null && interstitialAd.CanShowAd())
//            {
//                MSLog.Log("[ADMOB] Showing interstitial ad");
//                interstitialAd.Show();
//            }
//            else
//            {
//                MSLog.LogError("[ADMOB] Interstitial ad is not ready yet");
//                _interTcs?.TrySetResult(AdOutcome.NotAvailable);
//            }
//        }

//        private void RegisterEventHandlers(InterstitialAd ad)
//        {
//            // Raised when the ad is estimated to have earned money
//            ad.OnAdPaid += (AdValue adValue) =>
//            {
//                MSLog.Log(String.Format("Interstitial ad paid {0} {1}", adValue.Value, adValue.CurrencyCode));
//            };
//            // Raised when an impression is recorded for an ad
//            ad.OnAdImpressionRecorded += () =>
//            {
//                MSLog.Log("Interstitial ad recorded an impression");
//            };
//            // Raised when a click is recorded for an ad
//            ad.OnAdClicked += () =>
//            {
//                MSLog.Log("Interstitial ad was clicked.");
//            };
//            // Raised when an ad opened full screen content
//            ad.OnAdFullScreenContentOpened += () =>
//            {
//                MSLog.Log("Interstitial ad full screen content opened");
//            };
//            // Raised when the ad closed full screen content
//            ad.OnAdFullScreenContentClosed += () =>
//            {
//                MSLog.Log("[ADMOB] Interstitial closed");
//                _interTcs?.TrySetResult(AdOutcome.Completed);  // 정상 닫힘(노출 완료)
//                _interstitialReady = false;
//                LoadAd_Interstital();           // 다음 회차 준비
//            };
//            // Raised when the ad failed to open full screen content
//            ad.OnAdFullScreenContentFailed += (AdError error) =>
//            {
//                MSLog.LogError($"[ADMOB] Interstitial open failed: {error}");
//                _interTcs?.TrySetResult(AdOutcome.Failed); // 실패
//                _interstitialReady = false;
//                LoadAd_Interstital();
//            };
//        }

//        private void StartInterstitialRetryLoop()
//        {
//            if (_interstitialRetryCo != null)
//                return; // 중복 시작 방지

//            _isInterstitialRetrying = true;
//            _interstitialRetryCo = StartCoroutine(Co_ScheduleInterstitialRetry());
//        }

//        private void StopInterstitialRetryLoop()
//        {
//            _isInterstitialRetrying = false;

//            if (_interstitialRetryCo != null)
//            {
//                StopCoroutine(_interstitialRetryCo);
//                _interstitialRetryCo = null;
//            }
//        }

//        private IEnumerator Co_ScheduleInterstitialRetry()
//        {
//            int delay = _baseInterstitialDelay;

//            while (!_interstitialReady)
//            {
//                if (_interstitialRetryCount >= MaxInterstitialRetryCount)
//                {
//                    MSLog.Log("[ADMOB] Interstitial max retry reached, stop auto retry.");
//                    _interstitialRetryCo = null;
//                    _isInterstitialRetrying = false;
//                    yield break;
//                }

//                // 이미 로드 요청 중이면 다음 프레임까지 대기
//                if (_isInterstitialLoading)
//                {
//                    yield return null;
//                    continue;
//                }

//                MSLog.Log($"[ADMOB] Interstitial retry wait {delay}s (count={_interstitialRetryCount})");
//                yield return new WaitForSecondsRealtime(delay);

//                // 기다리는 동안 준비되었으면 종료
//                if (_interstitialReady)
//                    break;

//                _interstitialRetryCount++;

//                LoadAd_Interstital();
//            }

//            // 준비되면 루프 종료
//            _interstitialRetryCo = null;
//            _isInterstitialRetrying = false;
//        }

//        #endregion Interstitial_AD

//        #region Rewarded_AD

//        private RewardedAd rewardedAd;
//        private Action _pendingRewardAction;

//        public void LoadAd_Rewarded()
//        {
//            if (_isRewardedLoading_1)
//                return; // 중복 로드 방지

//            _isRewardedLoading_1 = true;

//            rewardedAd?.Destroy();
//            rewardedAd = null;
//            _rewardedReady = false;

//            var adRequest = new AdRequest();
//            RewardedAd.Load(adUnitId_Rewarded, adRequest, (RewardedAd ad, LoadAdError error) =>
//            {
//                _isRewardedLoading_1 = false;

//                if (error != null || ad == null)
//                {
//                    MSLog.LogError($"[ADMOB] Rewarded load failed: {error}");
//                    _rewardedReady = false;

//                    StartRewardedRetryLoop(); // 실패 시 루프 시작(중복 방지)
//                    return;
//                }

//                rewardedAd = ad;
//                _rewardedReady = true;
//                _rewardedRetryCount_1 = 0;

//                RegisterEventHandlers(rewardedAd);
//                MSLog.Log($"[ADMOB] Rewarded loaded: {ad.GetResponseInfo()}");

//                StopRewardedRetryLoop(); // 성공하면 루프 종료
//            });
//        }

//        public void ShowAd_Rewarded(Action rewardCall)
//        {
//            if (rewardedAd != null && rewardedAd.CanShowAd())
//            {
//                _wasRewardedThisShow = false;
//                _pendingRewardAction = rewardCall;

//                rewardedAd.Show((Reward reward) =>
//                {
//                    _wasRewardedThisShow = true;
//                    _rewardTcs?.TrySetResult(AdOutcome.Completed);
//                });
//            }
//            else
//            {
//                _rewardTcs?.TrySetResult(AdOutcome.NotAvailable);
//            }
//        }

//        private void RegisterEventHandlers(RewardedAd ad)
//        {
//            // Raised when the ad is estimated to have earned money
//            ad.OnAdPaid += (AdValue adValue) =>
//            {
//                MSLog.Log(String.Format("[ADMOB] Rewarded ad paid {0} {1}", adValue.Value, adValue.CurrencyCode));
//            };
//            // Raised when an impression is recorded for an ad
//            ad.OnAdImpressionRecorded += () =>
//            {
//                MSLog.Log("[ADMOB] Rewarded ad recorded an impression");
//            };
//            // Raised when a click is recorded for an ad
//            ad.OnAdClicked += () =>
//            {
//                MSLog.Log("[ADMOB] Rewarded ad was clicked");
//            };
//            // Raised when an ad opened full screen content
//            ad.OnAdFullScreenContentOpened += () =>
//            {
//                // 배너 숨김 여기서 해야함 
//                MSLog.Log("[ADMOB] Rewarded ad full screen content opened");
//            };
//            // Raised when the ad closed full screen content
//            ad.OnAdFullScreenContentClosed += () =>
//            {
//                MSLog.Log("[ADMOB] Rewarded closed");

//                // 결과 세팅은 해도 되지만, UI/리소스/Instantiate는 메인 스레드로
//                MainThreadDispatcher.Post(() =>
//                {
//                    // (권장) 광고 닫힌 직후 한 프레임 쉬고 UI 처리
//                    GameCore.Instance.StartCoroutine(WaitFocusThen(() =>
//                    {
//                        if (!_wasRewardedThisShow)
//                            _rewardTcs?.TrySetResult(AdOutcome.Skipped);
//                        else
//                            _pendingRewardAction?.Invoke(); // <- 여기서 팝업 닫기/열기

//                        _pendingRewardAction = null;

//                        _rewardedReady = false;
//                        LoadAd_Rewarded(); // 이것도 내부에서 Unity API 쓰면 같이 메인스레드로
//                    }));
//                });
//            };

//            // Raised when the ad failed to open full screen content
//            ad.OnAdFullScreenContentFailed += (AdError error) =>
//            {
//                MSLog.LogError($"[ADMOB] Rewarded open failed: {error}");
//                _rewardTcs?.TrySetResult(AdOutcome.Failed);   // 실패 결과 전달
//                _rewardedReady = false;
//                LoadAd_Rewarded();                            // 다음 회차를 위해 미리 로드
//            };
//        }

//        private IEnumerator WaitFocusThen(Action a)
//        {
//            // 포커스 돌아올 때까지 대기
//            while (!Application.isFocused)
//                yield return null;

//            // 한 프레임 더
//            yield return null;

//            a?.Invoke();
//        }

//        private void StartRewardedRetryLoop()
//        {
//            if (_rewardedRetryCo_1 != null)
//                return; // 이미 돌고 있으면 중복 시작 금지

//            _isRewardedRetrying_1 = true;
//            _rewardedRetryCo_1 = StartCoroutine(Co_ScheduleRewardedRetry());
//        }

//        private void StopRewardedRetryLoop()
//        {
//            _isRewardedRetrying_1 = false;
//            if (_rewardedRetryCo_1 != null)
//            {
//                StopCoroutine(_rewardedRetryCo_1);
//                _rewardedRetryCo_1 = null;
//            }
//        }

//        private IEnumerator Co_ScheduleRewardedRetry()
//        {
//            int delay = _baseRewardedDelay_1;

//            while (!_rewardedReady)
//            {
//                if (_rewardedRetryCount_1 >= MaxRewardedRetryCount_1)
//                {
//                    MSLog.Log("[ADMOB] Rewarded max retry reached, stop auto retry.");
//                    _rewardedRetryCo_1 = null;
//                    _isRewardedRetrying_1 = false;
//                    _baseRewardedDelay_1 *= 2;
//                    yield break;
//                }

//                // 로드 중이면 기다렸다가 다음 루프로
//                if (_isRewardedLoading_1)
//                {
//                    yield return null;
//                    continue;
//                }

//                MSLog.Log($"[ADMOB] Rewarded retry wait {delay}s (count={_rewardedRetryCount_1})");
//                yield return new WaitForSecondsRealtime(delay);

//                // 기다리는 동안 이미 준비됐으면 종료
//                if (_rewardedReady)
//                    break;

//                _rewardedRetryCount_1++;

//                // 다음 딜레이: 15, 30
//                delay = Math.Min(delay * 2, 30);

//                LoadAd_Rewarded();
//            }

//            // 준비되면 루프 종료
//            _rewardedRetryCo_1 = null;
//            _isRewardedRetrying_1 = false;
//        }

//        #endregion Rewarded_AD

//        #region GDPR Logic (UMP)

//        private void OnConsentInfoUpdated(FormError consentError)
//        {
//            MSLog.Log("[GDPR] 1. Agree State Callback Check");
//            if (consentError != null)
//            {
//                MSLog.Log("[GDPR] Agree State Check Failed : " + consentError);
//                _IsInitGDPR = true;
//                return;
//            }
//            MSLog.Log("[GDPR] 2. Agree State Check Success!");

//            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
//            {
//                MSLog.Log("[GDPR] 3. Form Load Try");
//                if (formError != null)
//                {
//                    MSLog.Log("[GDPR] Form Get Failed : " + consentError);
//                    _IsInitGDPR = true;
//                    return;
//                }

//                //if (CurrentGdpr.IsGDPR()) // GDPR을 검사하는 국가에서만 체크
//                //{
//                //    isNoAd = !CurrentGdpr.CanAdShow();
//                //    if (CurrentGdpr.IsPartnerConsent("1301")) // 앱로빈 확인
//                //    {
//                //        AppLovin.SetHasUserConsent(true);
//                //        MSLog.Log("앱로빈 GDPR 켜짐");
//                //    }

//                //    if (CurrentGdpr.IsPartnerConsent("3234")) // 유니티애즈 확인
//                //    {
//                //        UnityAds.SetConsentMetaData("gdpr.consent", true);
//                //        MSLog.Log("유니티애즈 GDPR 켜짐");
//                //    }
//                //}
//                //else
//                //    isNoAd = false;

//                //MSLog.Log("[GDPR] 4. GDPR.IsGDPR : " + CurrentGDPR.IsGDPR());
//                //MSLog.Log("[GDPR] 5. CanShowPersonalizedAds? " + CurrentGDPR.CanShowPersonalizedAds());

//                if (ConsentInformation.CanRequestAds())
//                {
//                    // 구글 애즈 초기화
//                    MSLog.Log("[ADMOB] MobileAds Init");
//                    MobileAds.Initialize(initStatus =>
//                    {
//                        var map = initStatus.getAdapterStatusMap();
//                        foreach (var entry in map)
//                        {
//                            string adapterName = entry.Key;
//                            AdapterStatus adapterStatus = entry.Value;

//                            MSLog.Log($"[ADMOB ADAPTER] {adapterName} = {adapterStatus.InitializationState}, Desc = {adapterStatus.Description}");
//                        }

//                        LoadAd_Banner();
//                        LoadAd_Interstital();
//                        LoadAd_Rewarded();
//                        //LoadAd_Rewarded_Spare();

//                        GameConfig.Get(GameConfig.ADS_INTERSTITIAL, ref _NowInterstitialCount);
//                        _IsInitGDPR = true;
//                    });
//                }
//                else
//                {
//                    MSLog.Log("[GDPR] Ads Request Failed - State");
//                    _IsInitGDPR = true;
//                }
//            });
//        }

//        #endregion GDPR Logic (UMP)

//        #region IDFA Logic (IOS)

//        /// <summary>
//        /// Request ATT(IDFA)
//        /// [Package] iOS 14 Advertising Support
//        /// </summary>
//        public void RequestAuthorizationTracking()
//        {
//#if UNITY_IOS
//            MSLog.Log("[IDFA] Start");     
//            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
//            MSLog.Log($"[IDFA] AuthorizationTrackingStatus: {status}");

//            // 결정되지 않은 상태일 때만 체크 및 요청
//            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
//            {
//                bool bNeedRequest = false;
//                if (!CurrentGDPR.IsGDPR())
//                {
//                    // [Non EEA] ATT 동의 요청
//                    bNeedRequest = true;
//                    MSLog.Log("[IDFA] Non EEA -> ATT Agree");
//                }
//                else
//                {
//                    // [EEA]
//                    if (CurrentGDPR.IsPurposeConsents())
//                    {
//                        // [GDPR 동의] ATT 동의 요청
//                        bNeedRequest = true;
//                        MSLog.Log("[IDFA] ATT Request Yes");
//                    }
//                    else
//                    {
//                        // [GDPR 비동의] ATT 동의 체크가 필요없음
//                        bNeedRequest = false;
//                        MSLog.Log("[IDFA] ATT Request No");
//                    }
//                }

//                // ATT 동의 요청 필요
//                if (bNeedRequest)
//                {
//                    ATTrackingStatusBinding.RequestAuthorizationTracking();
//                    MSLog.Log("[IDFA] RequestAuthorizationTracking Flow~");
//                }
//            }

//            _ISInitIDFA = true;
//#else
//            MSLog.Log("[IDFA] Unity iOS Support: Tried to request iOS App Tracking Transparency native dialog, " + "but the current platform is not iOS.");
//            _ISInitIDFA = true;
//#endif
//        }

//        #endregion IDFA Logic (IOS)
//    }
//}