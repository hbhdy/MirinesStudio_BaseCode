using System;
using Cysharp.Threading.Tasks;
#if MIRINES_FIREBASE
using Firebase;
using Firebase.Firestore;
using UnityEngine;
#endif

namespace MirinesStudio
{
    public class FirestoreAdapter : BaseManager, IGameVersionSource
    {
        public bool IsReady => isReady;

#if MIRINES_FIREBASE
        private FirebaseManager manager;

        public FirebaseManager Manager => IsReady ? manager : null;

        public override async UniTask Task_Init()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            try
            {
                DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask().AttachExternalCancellation(cancellationToken);
                if (status != DependencyStatus.Available)
                {
                    Debug.LogWarning($"Firestore initialization skipped: {status}", this);
                    return;
                }

                manager = new FirebaseManager(FirebaseFirestore.DefaultInstance);
                await base.Task_Init();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firestore initialization failed: {e.Message}", this);
            }
        }
#else
        public override UniTask Task_Init() => UniTask.CompletedTask;
#endif

        public override void Init()
        {
            if (IsReady)
                base.Init();
        }

        public UniTask<string> FetchVersionPassAsync()
        {
#if MIRINES_FIREBASE
            FirebaseManager readyManager = Manager;
            if (readyManager == null)
                throw new InvalidOperationException("FirestoreAdapter is not ready.");

            return readyManager.FetchVersionPassAsync();
#else
            throw new InvalidOperationException("FirestoreAdapter requires the MIRINES_FIREBASE scripting define.");
#endif
        }

        public override void Shutdown()
        {
            isReady = false;
#if MIRINES_FIREBASE
            manager = null;
#endif
            base.Shutdown();
        }
    }
}
