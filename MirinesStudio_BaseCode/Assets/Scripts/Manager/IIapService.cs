using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MirinesStudio
{
    public enum IapResult
    {
        Success,
        Pending,
        Cancelled,
        Failed,
        NotFound,
        NotReady,
    }

    public enum IapProductType
    {
        Consumable,
        NonConsumable,
        Subscription,
    }

    [Serializable]
    public struct IapProductEntry
    {
        public string productId;
        public IapProductType type;
    }

    public readonly struct IapOrderItem
    {
        public string ProductId { get; }
        public int Quantity { get; }

        public IapOrderItem(string productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }
    }

    public sealed class IapOrder
    {
        public string StoreName { get; }
        public string TransactionId { get; }
        public string Receipt { get; }
        public string AppleJws { get; }
        public IReadOnlyList<IapOrderItem> Items { get; }

        public IapOrder(string storeName, string transactionId, string receipt, string appleJws, IReadOnlyList<IapOrderItem> items)
        {
            StoreName = storeName;
            TransactionId = transactionId;
            Receipt = receipt;
            AppleJws = appleJws;
            Items = items;
        }
    }

    public interface IIapOrderHandler
    {
        // Return true only after validation and durable delivery, including a previously delivered transaction.
        UniTask<bool> FulfillAsync(IapOrder order);

        // Reconcile restorable entitlements. Do not grant consumables again from this snapshot.
        void RestoreEntitlements(IReadOnlyList<IapOrder> confirmedOrders);
    }

    public interface IIapService
    {
        bool IsReady { get; }
        bool TryGetLocalizedPrice(string productId, out string price);
        UniTask<IapResult> PurchaseAsync(string productId, CancellationToken ct = default);
        bool RestorePurchases();
    }
}
