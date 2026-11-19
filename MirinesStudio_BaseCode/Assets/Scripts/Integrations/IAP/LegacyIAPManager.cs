//using Cysharp.Threading.Tasks;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using UnityEngine.Purchasing;

//namespace MirinesStudio
//{
//    public enum IapResult
//    {
//        Success,     // 지급 완료 + Confirm 성공
//        Pending,     // 보상 지급 전(대기)
//        Cancelled,   // 유저 취소
//        Failed,      // 실패(네트워크/스토어 에러 등)
//        NotFound,    // 제품 미정의
//        NotReady     // 초기화 전/스마트폰 미연결 등
//    }

//    public class IAPManager : BaseManager
//    {
//        // ----- Param -----

//        // 스토어 콘솔에 등록한 상품 ID
//        public const string PRODUCT_UNDO_S = "catsort.item_undo_s";
//        public const string PRODUCT_UNDO_M = "catsort.item_undo_m";
//        public const string PRODUCT_UNDO_L = "catsort.item_undo_l";
//        public const string PRODUCT_EXTRA_S = "catsort.item_slotadd_s";
//        public const string PRODUCT_EXTRA_M = "catsort.item_slotadd_m";
//        public const string PRODUCT_EXTRA_L = "catsort.item_slotadd_l";
//        public const string PRODUCT_ADS = "catsort.no_ads";                 // 광고 제거
//        public const string PRODUCT_ADS_LIMITED = "catsort.limited_offer";  // 광고 제거 + 아이템 번들

//        // 1.1.0 추가
//        public const string PRODUCT_ADS_TIME_1 = "catsort.time_no_ads_1d";   // 1일 광고 제거
//        public const string PRODUCT_ADS_TIME_3 = "catsort.time_no_ads_3d";   // 3일 광고 제거
//        public const string PRODUCT_ADS_TIME_7 = "catsort.time_no_ads_7d";   // 7일 광고 제거

//        private StoreController _store;
//        private bool _connected;
//        private readonly Dictionary<string, Product> _products = new();

//        // 보상 지급을 대기시키기 위한 TCS (구매 버튼에서 await용)
//        private UniTaskCompletionSource<IapResult> _purchaseTcs;
//        private string _pendingProductId;
//        private CancellationTokenSource _initCts;

//        public bool isProcessComplete = true;

//        // ----- Init -----

//        public override async UniTask Task_Init()
//        {
//            await base.Task_Init();
//        }

//        public override void Init()
//        {
//            base.Init();
//            InitializeIAP().Forget();
//        }

//        private async UniTaskVoid InitializeIAP()
//        {
//            _initCts = new CancellationTokenSource();

//            _store = UnityIAPServices.StoreController();

//            // 이벤트 연결 (먼저 연결해두고 Connect)
//            _store.OnPurchasePending += OnPurchasePending;
//            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
//            _store.OnPurchaseDeferred += OnPurchaseDeferred;
//            _store.OnProductsFetched += OnProductsFetched;
//            _store.OnProductsFetchFailed += OnProductsFetchFailed;
//            _store.OnPurchasesFetched += OnPurchasesFetched;
//            _store.OnPurchaseFailed += OnPurchaseFailed;
//            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
//            _store.OnStoreDisconnected += OnStoreDisconnected;

//            // Pending 자동 처리 OFF (직접 Confirm 전략)
//            _store.ProcessPendingOrdersOnPurchasesFetched(false);

//            // 연결 시도 (재시도 루프 예시)
//            for (int retry = 0; retry < 3 && !_connected; retry++)
//            {
//                try
//                {
//                    await _store.Connect();
//                    _connected = true;
//                }
//                catch (Exception e)
//                {
//                    MSLog.LogError($"[IAP] Connect failed: {e.Message}");
//                    await UniTask.Delay(TimeSpan.FromSeconds(2));
//                }
//            }

//            if (!_connected)
//            {
//                MSLog.LogError("[IAP] Failed to connect.");
//                return;
//            }

//            // 제품 목록 페치 -> 이어서 구매 내역 동기화
//            var defs = new List<ProductDefinition>
//            {
//                new(ProductId(PRODUCT_UNDO_S),        ProductType.Consumable),
//                new(ProductId(PRODUCT_UNDO_M),        ProductType.Consumable),
//                new(ProductId(PRODUCT_UNDO_L),        ProductType.Consumable),
//                new(ProductId(PRODUCT_EXTRA_S),       ProductType.Consumable),
//                new(ProductId(PRODUCT_EXTRA_M),       ProductType.Consumable),
//                new(ProductId(PRODUCT_EXTRA_L),       ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS),           ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS_LIMITED),   ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS_TIME_1),    ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS_TIME_3),    ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS_TIME_7),    ProductType.Consumable),
//            };

//            _store.FetchProducts(defs);
//        }

//        // ----- Set -----

//        // 스토어별 suffix/prefix가 있으면 여기서 변환
//        private string ProductId(string id) => id;

//        // ----- Get -----

//        public string GetLocalizedPrice(string productId)
//        {
//            return _products.TryGetValue(productId, out var p) && p?.metadata != null
//                ? p.metadata.localizedPriceString   // 예: 1,200
//                : "—";
//        }

//        private Payment GetCreatePayment(string productId, string orderId)
//        {
//            Product product = _products[productId];
//            Payment pm = new Payment();
//            pm.UserID = StaticGameData.UserInfo.ID;
//            pm.OrderID = orderId;
//            pm.Country = GameCore.FB.GetCountryCode();
//            pm.Price = MathF.Round(product.metadata.localizedPrice.ToFloat(), 3);
//            pm.ProductID = productId;
//            pm.Status = 1;
//            return pm;
//        }

//        private string editorOrderID;

//        public string GetOrderIDByReceipt(string Receipt)
//        {
//            // MiniJson을 사용하여 영수증 문자열 파싱
//            var receiptDict = (Dictionary<string, object>)MiniJson.JsonDecode(Receipt);

//#if UNITY_EDITOR
//            StringBuilder sb = new StringBuilder();
//            int rand = UnityEngine.Random.Range(0, 100000000);
//            sb.Append(StaticGameData.UserInfo.ID + "_" + rand.ToString());
//            editorOrderID = sb.ToString();
//            return sb.ToString();
//#else
//            if (receiptDict != null && receiptDict.ContainsKey("Payload"))
//            {
//                string payloadString = (string)receiptDict["Payload"];
//                MSLog.Log("Payload string: " + payloadString);

//                var payloadDict = (Dictionary<string, object>)MiniJson.JsonDecode(payloadString);

//                if (payloadDict != null && payloadDict.ContainsKey("json"))
//                {
//                    // "json" 키 아래에 실제 Google Play 영수증 데이터가 있음
//                    string jsonString = (string)payloadDict["json"];
//                    var jsonDict = (Dictionary<string, object>)MiniJson.JsonDecode(jsonString);

//                    if (jsonDict != null && jsonDict.ContainsKey("orderId"))
//                    {
//                        // 여기서 orderId 추출 성공
//                        string orderIdString = (string)jsonDict["orderId"];
//                        MSLog.Log("Order ID: " + orderIdString);
//                        return orderIdString;
//                    }
//                }
//            }
//             return "";
//#endif
//        }

//        // ----- Main -----

//        // ----- Logic -----

//        private void OnProductsFetched(List<Product> products)
//        {
//            _products.Clear();
//            foreach (var p in products)
//            {
//                _products[p.definition.id] = p;
//                MSLog.Log($"[IAP] Fetched: {p.definition.id} | {p.metadata.localizedTitle} | {p.metadata.localizedPriceString}");
//            }

//            // 과거 구매 동기화(구독/비소모 권한 회복)
//            _store.FetchPurchases();
//        }

//        private void OnProductsFetchFailed(ProductFetchFailed failed)
//        {
//        }

//        private void OnPurchasesFetched(Orders orders)
//        {
//            try
//            {
//                // Confirmed(이미 확정된 구매들: 구독/비소모 권한 유지)
//                if (orders?.ConfirmedOrders != null)
//                {
//                    foreach (var co in orders.ConfirmedOrders)
//                    {
//                        foreach (var pinfo in co.Info.PurchasedProductInfo)
//                        {
//                            var pid = pinfo.productId;
//                            MSLog.Log($"[IAP] ConfirmedOwned: {pid}");
//                            // 구독/비소모 권한 반영
//                            ApplyEntitlement(pid, grant: true);
//                        }
//                    }
//                }

//                // Pending(미확정 구매들: 우리가 Confirm해야 함)
//                if (orders?.PendingOrders != null)
//                {
//                    foreach (var po in orders.PendingOrders)
//                    {
//                        foreach (var pinfo in po.Info.PurchasedProductInfo)
//                        {
//                            var pid = pinfo.productId;
//                            MSLog.Log($"[IAP] PendingFoundOnFetch: {pid}");
//                            // 보상 지급 후 Confirm (자동화할지, UI 유도할지 정책에 따라)
//                            // 여기서는 자동 처리 예시:
//                            OnPurchasePending(po);
//                        }
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                MSLog.LogError($"[IAP] OnPurchasesFetched error: {e}");
//            }
//        }

//        private bool _isPurchasing;

//        /// <summary>
//        /// 구매 버튼을 눌렀을 때 호출되는 단일 엔트리
//        /// </summary>
//        public async UniTask<IapResult> PurchaseAsync(string productId, CancellationToken ct = default)
//        {
//            isProcessComplete = false;

//            if (!_connected)
//            {
//                isProcessComplete = true;
//                return IapResult.NotReady;
//            }

//            if (!_products.TryGetValue(productId, out var product) || product?.metadata == null)
//            {
//                isProcessComplete = true;
//                return IapResult.NotFound;
//            }
//            if (!product.availableToPurchase)
//            {
//                MSLog.LogError($"[IAP] Not available to purchase: {productId}");
//                isProcessComplete = true;
//                return IapResult.NotFound;
//            }

//            if (_isPurchasing) // 동시 구매 방지
//            {
//                isProcessComplete = true;
//                return IapResult.Failed;
//            }

//            _isPurchasing = true;

//            // 이전 대기 정리
//            _purchaseTcs?.TrySetResult(IapResult.Failed);
//            _purchaseTcs = new UniTaskCompletionSource<IapResult>();
//            _pendingProductId = productId;

//            CancellationTokenRegistration ctr = default;
//            if (ct.CanBeCanceled)
//                ctr = ct.Register(() => _purchaseTcs.TrySetResult(IapResult.Failed));

//            try
//            {
//                _store.PurchaseProduct(product);
//                var result = await _purchaseTcs.Task;
//                if (result != IapResult.Success)
//                    isProcessComplete = true;
//                return result;
//            }
//            catch (Exception e)
//            {
//                MSLog.LogError($"[IAP] Purchase call failed: {e}");
//                isProcessComplete = true;
//                return IapResult.Failed;
//            }
//            finally
//            {
//                MSLog.Log("[IAP] Purchase End");
//                _pendingProductId = null;
//                _isPurchasing = false;
//                ctr.Dispose();
//            }
//        }

//        /// <summary>
//        /// 구매가 보류(Pending) 상태로 들어오면 여기서 보상 지급 후 Confirm
//        /// </summary>
//        private async void OnPurchasePending(PendingOrder pending)
//        {
//            try
//            {
//                var pid = pending.Info.PurchasedProductInfo.Count > 0
//                    ? pending.Info.PurchasedProductInfo[0].productId
//                    : null;

//                if (string.IsNullOrEmpty(pid))
//                {
//                    MSLog.LogError("[IAP] Pending has no productId");
//                    _purchaseTcs?.TrySetResult(IapResult.Failed);
//                    return;
//                }

//                MSLog.Log($"[IAP] Pending: {pid}");

//                // (선택) 현재 요청한 상품과 다르면 별도로 처리
//                if (!string.IsNullOrEmpty(_pendingProductId) && _pendingProductId != pid)
//                {
//                    MSLog.Log($"[IAP] Pending product mismatch. expected:{_pendingProductId} got:{pid}");
//                }

//                var granted = await GrantBenefitsAsync(pid, pending);

//                if (!granted)
//                {
//                    MSLog.Log("[IAP] Pending Failed");
//                    _purchaseTcs?.TrySetResult(IapResult.Failed);
//                    return;
//                }
//                _store.ConfirmPurchase(pending); // v5: void

//                MSLog.Log($"[IAP] Confirmed: {pid}");
//                _purchaseTcs?.TrySetResult(IapResult.Success);
//            }
//            catch (OperationCanceledException)
//            {
//                _purchaseTcs?.TrySetResult(IapResult.Failed);
//            }
//            catch (Exception e)
//            {
//                MSLog.LogError($"[IAP] Pending handle error: {e}");
//                _purchaseTcs?.TrySetResult(IapResult.Failed);
//            }
//        }

//        private void OnPurchaseConfirmed(Order order)
//        {
//            // Confirm 이후에 스토어에서 확정 통지가 올 수 있음(보조 로깅/영수증 확보 용)
//            try
//            {
//                foreach (var p in order.Info.PurchasedProductInfo)
//                {
//                    MSLog.Log($"[IAP] OnPurchaseConfirmed: {p.productId} | receipt len: {order.Info.Receipt.Length}");

//                    switch (p.productId)
//                    {
//                        case PRODUCT_UNDO_S:
//                            StaticGameData.SetUndoItem_DB(StaticGameData.UserInfo.Undo + 25);
//                            break;
//                        case PRODUCT_UNDO_M:
//                            StaticGameData.SetUndoItem_DB(StaticGameData.UserInfo.Undo + 50);
//                            break;
//                        case PRODUCT_UNDO_L:
//                            StaticGameData.SetUndoItem_DB(StaticGameData.UserInfo.Undo + 125);
//                            break;
//                        case PRODUCT_EXTRA_S:
//                            StaticGameData.SetExtraItem(StaticGameData.UserInfo.ExtraSlot + 5);
//                            break;
//                        case PRODUCT_EXTRA_M:
//                            StaticGameData.SetExtraItem(StaticGameData.UserInfo.ExtraSlot + 10);
//                            break;
//                        case PRODUCT_EXTRA_L:
//                            StaticGameData.SetExtraItem(StaticGameData.UserInfo.ExtraSlot + 30);
//                            break;

//                        case PRODUCT_ADS:
//                            StaticGameData.SetUndoItem_DB(StaticGameData.UserInfo.Undo + 10);
//                            StaticGameData.SetExtraItem(StaticGameData.UserInfo.ExtraSlot + 2);
//                            StaticGameData.SetNoAds(true);
//                            GameCore.Instance.ShowAdsBannerIsOn(!StaticGameData.GetIsNoAds());
//                            break;
//                        case PRODUCT_ADS_LIMITED:
//                            StaticGameData.SetUndoItem_DB(StaticGameData.UserInfo.Undo + 10);
//                            StaticGameData.SetExtraItem(StaticGameData.UserInfo.ExtraSlot + 2);
//                            StaticGameData.SetNoAds(true);
//                            GameCore.Instance.ShowAdsBannerIsOn(!StaticGameData.GetIsNoAds());
//                            break;

//                        case PRODUCT_ADS_TIME_1:
//                            StaticGameData.LimitedBuyCheck();
//                            StaticGameData.SetNoAdsExpireDate(1);
//                            StaticGameData.SetUndoItem_DB(StaticGameData.UserInfo.Undo + 20);
//                            StaticGameData.SetExtraItem(StaticGameData.UserInfo.ExtraSlot + 5);
//                            StaticGameData.SetNoAds_Time(true);
//                            GameCore.Instance.ShowAdsBannerIsOn(!StaticGameData.GetIsNoAds());
//                            break;
//                        case PRODUCT_ADS_TIME_3:
//                            StaticGameData.SetNoAdsExpireDate(3);
//                            StaticGameData.SetNoAds_Time(true);
//                            GameCore.Instance.ShowAdsBannerIsOn(!StaticGameData.GetIsNoAds());
//                            break;
//                        case PRODUCT_ADS_TIME_7:
//                            StaticGameData.SetNoAdsExpireDate(7);
//                            StaticGameData.SetNoAds_Time(true);
//                            GameCore.Instance.ShowAdsBannerIsOn(!StaticGameData.GetIsNoAds());
//                            break;
//                    }

//#if UNITY_EDITOR
//                    GameCore.FB.UpdatePaymentAsync(StaticGameData.UserInfo.ID, editorOrderID, 0).Forget();
//#else
//                    GameCore.FB.UpdatePaymentAsync(StaticGameData.UserInfo.ID, GetOrderIDByReceipt(order.Info.Receipt), 0).Forget();
//#endif
//                    MSLog.Log("[IAP] Purchase Item FB Update");
//                }

//                isProcessComplete = true;
//                // 최초 지급한 아이템의 상태값 여기서 변경 해줌;
//            }
//            catch (Exception e)
//            {
//                MSLog.LogError($"[IAP] OnPurchaseConfirmed error: {e}");
//                isProcessComplete = true;
//            }
//        }

//        private void OnPurchaseDeferred(DeferredOrder d)
//        {
//            // Ask-To-Buy(가족결제) 등 지연 상태
//            MSLog.Log($"[IAP] Deferred: {d.Info?.PurchasedProductInfo?[0].productId}");
//            // 구매 UI/버튼 상태를 '승인 대기'로 표시하는 등 UX 처리
//        }

//        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription fail)
//        {
//            MSLog.LogError($"[IAP] FetchPurchases failed: {fail.failureReason} | {fail.Message}");
//        }

//        private async void OnStoreDisconnected(StoreConnectionFailureDescription desc)
//        {
//            MSLog.LogError($"[IAP] Store disconnected: {desc.Message} | retry...");
//            _connected = false;

//            // 단순 재시도 예시
//            for (int i = 0; i < 3 && !_connected; i++)
//            {
//                try
//                {
//                    await _store.Connect();
//                    _connected = true;
//                    _store.FetchProducts(new List<ProductDefinition>
//            {
//                new(ProductId(PRODUCT_UNDO_S),        ProductType.Consumable),
//                new(ProductId(PRODUCT_UNDO_M),        ProductType.Consumable),
//                new(ProductId(PRODUCT_UNDO_L),        ProductType.Consumable),
//                new(ProductId(PRODUCT_EXTRA_S),       ProductType.Consumable),
//                new(ProductId(PRODUCT_EXTRA_M),       ProductType.Consumable),
//                new(ProductId(PRODUCT_EXTRA_L),       ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS),           ProductType.Consumable),
//                new(ProductId(PRODUCT_ADS_LIMITED),   ProductType.Consumable),
//            });
//                }
//                catch (Exception e)
//                {
//                    await UniTask.Delay(TimeSpan.FromSeconds(2));
//                    MSLog.LogError($"[IAP] Reconnect failed: {e.Message}");
//                }
//            }
//        }

//        // 실패/취소 이벤트가 v5에 노출된다면 연결 (없으면 생략 가능)
//        private void OnPurchaseFailed(FailedOrder desc)
//        {
//            MSLog.LogError($"[IAP] Failed: {desc.CartOrdered} | {desc.FailureReason} | {desc.Details}");
//            _purchaseTcs?.TrySetResult(IapResult.Failed);
//        }

//        private void OnPurchaseCanceled(string productId)
//        {
//            MSLog.Log($"[IAP] Canceled: {productId}");
//            _purchaseTcs?.TrySetResult(IapResult.Cancelled);
//        }

//        /// <summary>
//        /// 제품별 보상/권한 부여. 실패 시 false 반환하여 Confirm 중단.
//        /// </summary>
//        private async UniTask<bool> GrantBenefitsAsync(string productId, PendingOrder order)
//        {
//            try
//            {
//                if (_products.ContainsKey(productId))
//                {
//                    await GameCore.FB.SavePaymentAsync(GetCreatePayment(productId, GetOrderIDByReceipt(order.Info.Receipt)));
//                    MSLog.Log("[IAP] Purchase Item FB Create");
//                    return true;
//                }

//                // 알 수 없는 제품: 거부
//                return false;
//            }
//            catch (Exception e)
//            {
//                MSLog.LogError($"[IAP] Grant failed: {productId} | {e}");
//                return false;
//            }
//            finally
//            {
//                await UniTask.Yield(); // 메인루프로 한 틱 양보(안정성)
//            }
//        }

//        private void ApplyEntitlement(string productId, bool grant)
//        {
//            //if (productId == PRODUCT_EXTRA_SLOT)
//            //{
//            //    if (grant) 
//            //        GameCore.PlayerData.MarkExtraSlotOwned();
//            //    else 
//            //        GameCore.PlayerData.MarkExtraSlotRevoked();
//            //}
//            //else if (productId == PRODUCT_SUB_VIP)
//            //{
//            //    if (grant) 
//            //        GameCore.PlayerData.SetVipActive(true);
//            //    else
//            //        GameCore.PlayerData.SetVipActive(false);
//            //}
//        }

//        // ----- Query Helpers -----

//        public bool TryGetLocalizedPrice(string productId, out string price)
//        {
//            if (_products.TryGetValue(productId, out var p) && p?.metadata != null)
//            {
//                price = p.metadata.localizedPriceString;
//                return true;
//            }
//            price = null;
//            return false;
//        }

//        public void RestorePurchases()
//        {
//            if (!_connected) return;
//            _store.FetchPurchases(); // v5는 이 호출로 동기화
//        }

//        private void OnDestroy()
//        {
//            _initCts?.Cancel();
//            _initCts?.Dispose();

//            if (_store != null)
//            {
//                _store.OnPurchasePending -= OnPurchasePending;
//                _store.OnProductsFetched -= OnProductsFetched;
//                _store.OnPurchasesFetched -= OnPurchasesFetched;
//                _store.OnPurchaseFailed -= OnPurchaseFailed;
//            }
//        }
//    }
//}
