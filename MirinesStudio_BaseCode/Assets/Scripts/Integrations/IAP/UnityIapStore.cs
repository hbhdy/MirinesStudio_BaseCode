#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace MirinesStudio
{
    internal sealed class UnityIapStore : IDisposable
    {
        private sealed class PurchaseRequest
        {
            public readonly string ProductId;
            public readonly UniTaskCompletionSource<IapResult> Completion = new UniTaskCompletionSource<IapResult>();
            public string TransactionId;

            public PurchaseRequest(string productId) => ProductId = productId;
        }

        private readonly List<ProductDefinition> definitions = new List<ProductDefinition>();
        private readonly Dictionary<string, Product> products = new Dictionary<string, Product>(StringComparer.Ordinal);
        private readonly HashSet<string> processingTransactions = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> confirmingTransactions = new HashSet<string>(StringComparer.Ordinal);
        private readonly IIapOrderHandler handler;
        private StoreController store;
        private PurchaseRequest activePurchase;
        private bool connected;
        private bool purchasesSynced;
        private bool syncInProgress;
        private bool disposed;

        public bool IsReady => connected && purchasesSynced && products.Count > 0;

        public UnityIapStore(IapProductEntry[] entries, IIapOrderHandler handler)
        {
            this.handler = handler;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (IapProductEntry entry in entries ?? Array.Empty<IapProductEntry>())
            {
                if (string.IsNullOrWhiteSpace(entry.productId) || !ids.Add(entry.productId))
                {
                    Debug.LogWarning($"[IAP] Skipping an empty or duplicate product ID: '{entry.productId}'.");
                    continue;
                }
                ProductType type = entry.type switch
                {
                    IapProductType.Consumable => ProductType.Consumable,
                    IapProductType.NonConsumable => ProductType.NonConsumable,
                    IapProductType.Subscription => ProductType.Subscription,
                    _ => ProductType.Unknown,
                };
                if (type == ProductType.Unknown)
                {
                    Debug.LogWarning($"[IAP] Skipping product '{entry.productId}' with an invalid product type.");
                    continue;
                }
                definitions.Add(new ProductDefinition(entry.productId, type));
            }
        }

        public async UniTask InitializeAsync()
        {
            if (definitions.Count == 0)
            {
                Debug.LogWarning("[IAP] Add at least one product to UnityIapAdapter.");
                return;
            }

            MSLog.Log("[IAP] Initialization started.");
            try
            {
                store = UnityIAPServices.StoreController();
                store.OnStoreConnected += OnStoreConnected;
                store.OnStoreDisconnected += OnStoreDisconnected;
                store.OnProductsFetched += OnProductsFetched;
                store.OnProductsFetchFailed += OnProductsFetchFailed;
                store.OnPurchasesFetched += OnPurchasesFetched;
                store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                store.OnPurchasePending += OnPurchasePending;
                store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                store.OnPurchaseFailed += OnPurchaseFailed;
                store.OnPurchaseDeferred += OnPurchaseDeferred;
                store.ProcessPendingOrdersOnPurchasesFetched(false);
                await store.Connect();
            }
            catch (Exception e)
            {
                if (!disposed)
                    Debug.LogWarning($"[IAP] Initialization failed: {e.Message}");
            }
        }

        private void OnStoreConnected()
        {
            if (disposed)
                return;
            connected = true;
            purchasesSynced = false;
            products.Clear();
            store.FetchProducts(definitions);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription error)
        {
            connected = false;
            purchasesSynced = false;
            syncInProgress = false;
            PurchaseRequest request = activePurchase;
            FinishPurchase(request, request?.TransactionId != null ? IapResult.Pending : IapResult.NotReady);
            Debug.LogWarning($"[IAP] Store disconnected: {error.Message}");
        }

        private void OnProductsFetched(List<Product> fetchedProducts)
        {
            if (!connected || disposed)
                return;
            foreach (Product product in fetchedProducts)
                products[product.uSku] = product;

            if (products.Count > 0)
                StartPurchaseSync(false);
        }

        private void OnProductsFetchFailed(ProductFetchFailed error) =>
            Debug.LogWarning($"[IAP] Product fetch failed: {error.FailureReason}");

        public bool TryGetLocalizedPrice(string productId, out string price)
        {
            price = null;
            if (!connected || string.IsNullOrEmpty(productId) || !products.TryGetValue(productId, out Product product))
                return false;

            price = product.metadata?.localizedPriceString;
            return !string.IsNullOrEmpty(price);
        }

        public bool RestorePurchases()
        {
            if (!connected || products.Count == 0 || activePurchase != null)
                return false;
            return StartPurchaseSync(Application.platform == RuntimePlatform.IPhonePlayer);
        }

        private bool StartPurchaseSync(bool restoreFromApple)
        {
            if (syncInProgress)
                return false;

            syncInProgress = true;
            try
            {
                if (restoreFromApple)
                {
                    store.RestoreTransactions((success, error) =>
                    {
                        if (!success && !disposed)
                        {
                            syncInProgress = false;
                            Debug.LogWarning($"[IAP] Restore failed: {error}");
                        }
                    });
                }
                else
                    store.FetchPurchases();
                return true;
            }
            catch (Exception e)
            {
                syncInProgress = false;
                Debug.LogWarning($"[IAP] Purchase sync failed: {e.Message}");
                return false;
            }
        }

        private void OnPurchasesFetched(Orders orders)
        {
            syncInProgress = false;
            if (disposed || !connected)
                return;

            bool firstSync = !purchasesSynced;
            purchasesSynced = true;
            if (firstSync)
                MSLog.Log("[IAP] Initialization complete.");

            if (handler != null)
            {
                var confirmed = new List<IapOrder>();
                foreach (ConfirmedOrder order in orders.ConfirmedOrders)
                    confirmed.Add(ToIapOrder(order));
                try { handler.RestoreEntitlements(confirmed); }
                catch (Exception e) { Debug.LogWarning($"[IAP] Entitlement sync failed: {e.Message}"); }
            }

            foreach (PendingOrder order in orders.PendingOrders)
                ProcessPendingAsync(order, null).Forget();
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription error)
        {
            syncInProgress = false;
            Debug.LogWarning($"[IAP] Purchase fetch failed: {error.Message}");
        }

        public async UniTask<IapResult> PurchaseAsync(string productId, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return IapResult.Cancelled;
            if (!IsReady || handler == null || syncInProgress || activePurchase != null)
                return IapResult.NotReady;
            if (string.IsNullOrEmpty(productId) || !products.TryGetValue(productId, out Product product) || !product.availableToPurchase)
                return IapResult.NotFound;

            var request = new PurchaseRequest(productId);
            activePurchase = request;
            try
            {
                store.PurchaseProduct(product);
                return await request.Completion.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cancelling the UI wait does not cancel a transaction already sent to the store.
                return IapResult.Cancelled;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Purchase failed: {e.Message}");
                FinishPurchase(request, IapResult.Failed);
                return IapResult.Failed;
            }
        }

        private void OnPurchasePending(PendingOrder order) => ProcessPendingAsync(order, MatchNewPurchase(order)).Forget();

        private async UniTask ProcessPendingAsync(PendingOrder pending, PurchaseRequest request)
        {
            if (disposed)
                return;

            IapOrder order = ToIapOrder(pending);
            string transactionId = order.TransactionId;
            if (handler == null || string.IsNullOrWhiteSpace(transactionId) || order.Items.Count == 0)
            {
                FinishPurchase(request, IapResult.Pending);
                return;
            }
            if (request != null)
                request.TransactionId = transactionId;
            if (!processingTransactions.Add(transactionId))
                return;

            bool confirmationRequested = false;
            try
            {
                bool fulfilled = await handler.FulfillAsync(order);
                await UniTask.SwitchToMainThread();
                if (disposed)
                    return;
                if (!fulfilled || !connected)
                {
                    FinishPurchase(MatchTransaction(transactionId), IapResult.Pending);
                    return;
                }

                confirmingTransactions.Add(transactionId);
                store.ConfirmPurchase(pending);
                confirmationRequested = true;
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                confirmingTransactions.Remove(transactionId);
                if (!disposed)
                {
                    Debug.LogWarning($"[IAP] Order fulfilment or confirmation failed: {e.Message}");
                    FinishPurchase(MatchTransaction(transactionId), IapResult.Pending);
                }
            }
            finally
            {
                if (!confirmationRequested)
                    processingTransactions.Remove(transactionId);
            }
        }

        private void OnPurchaseConfirmed(Order order)
        {
            string transactionId = order.Info.TransactionID;
            if (!confirmingTransactions.Remove(transactionId))
                return;

            processingTransactions.Remove(transactionId);
            bool confirmed = order is ConfirmedOrder;
            if (order is FailedOrder failed)
                Debug.LogWarning($"[IAP] Store did not confirm the fulfilled order: {failed.FailureReason}");
            FinishPurchase(MatchTransaction(transactionId), confirmed ? IapResult.Success : IapResult.Pending);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Debug.LogWarning($"[IAP] Store purchase failed: {order.FailureReason}");
            FinishPurchase(MatchNewPurchase(order), order.FailureReason == PurchaseFailureReason.UserCancelled ? IapResult.Cancelled : IapResult.Failed);
        }

        private void OnPurchaseDeferred(DeferredOrder order) => FinishPurchase(MatchNewPurchase(order), IapResult.Pending);

        private PurchaseRequest MatchNewPurchase(Order order)
        {
            PurchaseRequest request = activePurchase;
            if (request == null || request.TransactionId != null)
                return null;
            foreach (CartItem item in order.CartOrdered.Items())
            {
                if (item.Product.uSku == request.ProductId)
                    return request;
            }
            return null;
        }

        private PurchaseRequest MatchTransaction(string transactionId) =>
            activePurchase?.TransactionId == transactionId ? activePurchase : null;

        private void FinishPurchase(PurchaseRequest request, IapResult result)
        {
            if (request == null || activePurchase != request)
                return;
            activePurchase = null;
            request.Completion.TrySetResult(result);
        }

        private static IapOrder ToIapOrder(Order order)
        {
            var items = new List<IapOrderItem>();
            foreach (CartItem item in order.CartOrdered.Items())
                items.Add(new IapOrderItem(item.Product.uSku, item.Quantity));

            return new IapOrder(order.StoreName, order.Info.TransactionID, order.Info.Receipt, order.Info.Apple?.jwsRepresentation, items);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            connected = false;
            purchasesSynced = false;
            FinishPurchase(activePurchase, IapResult.NotReady);
            if (store == null)
                return;

            store.OnStoreConnected -= OnStoreConnected;
            store.OnStoreDisconnected -= OnStoreDisconnected;
            store.OnProductsFetched -= OnProductsFetched;
            store.OnProductsFetchFailed -= OnProductsFetchFailed;
            store.OnPurchasesFetched -= OnPurchasesFetched;
            store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            store.OnPurchasePending -= OnPurchasePending;
            store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            store.OnPurchaseFailed -= OnPurchaseFailed;
            store.OnPurchaseDeferred -= OnPurchaseDeferred;
            processingTransactions.Clear();
            confirmingTransactions.Clear();
            products.Clear();
            store = null;
        }
    }
}
#endif
