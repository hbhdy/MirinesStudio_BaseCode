using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace MirinesStudio
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private static MainThreadDispatcher _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var go = new GameObject("[MainThreadDispatcher]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
        }

        public static void Post(Action action)
        {
            if (action == null) return;
            _queue.Enqueue(action);
        }

        private void Update()
        {
            while (_queue.TryDequeue(out var a))
            {
                try { a.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
