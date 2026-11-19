using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MirinesStudio
{
    public class UnityIapAdapter : BaseManager, IIapService
    {
        [SerializeField] private IapProductEntry[] products = new IapProductEntry[0];
        [SerializeField] private MonoBehaviour orderHandler;

#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
        private UnityIapStore store;
        public bool IsReady => store?.IsReady == true;
#else
        public bool IsReady => false;
#endif

        public override UniTask Task_Init() => UniTask.CompletedTask;

        public override void Init()
        {
#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            if (store != null)
                return;

            var handler = orderHandler as IIapOrderHandler;
            if (handler == null)
                Debug.LogWarning("[IAP] No IIapOrderHandler is assigned. Purchases are disabled.", this);

            store = new UnityIapStore(products, handler);
            store.InitializeAsync().Forget();
#endif
        }

        public bool TryGetLocalizedPrice(string productId, out string price)
        {
#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            if (store != null)
                return store.TryGetLocalizedPrice(productId, out price);
#endif
            price = null;
            return false;
        }

        public UniTask<IapResult> PurchaseAsync(string productId, CancellationToken ct = default)
        {
#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            if (store != null)
                return store.PurchaseAsync(productId, ct);
#endif
            return UniTask.FromResult(IapResult.NotReady);
        }

        public bool RestorePurchases()
        {
#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            return store?.RestorePurchases() == true;
#else
            return false;
#endif
        }

        public override void Shutdown()
        {
#if MIRINES_IAP && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)
            store?.Dispose();
            store = null;
#endif
            base.Shutdown();
        }

        private void OnDestroy() => Shutdown();
    }
}
