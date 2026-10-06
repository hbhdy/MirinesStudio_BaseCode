using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace MirinesStudio
{
    public class SceneContext : MonoBehaviour
    {
        // ----- Param -----

        [SerializeField]
        private Camera mainCamera;
        [SerializeField]
        private CanvasRoot canvasRoot;

        // ----- Init -----

        public virtual async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (canvasRoot == null)
                canvasRoot = CanvasRoot.instance;

            if (mainCamera == null)
                throw new InvalidOperationException($"Main Camera is missing in scene: {gameObject.scene.name}");

            if (canvasRoot == null)
                throw new InvalidOperationException($"CanvasRoot is missing in scene: {gameObject.scene.name}");

            if (GameCore.UI == null || !GameCore.UI.BindSceneReferences(mainCamera, canvasRoot))
                throw new InvalidOperationException($"UI scene references are invalid: {gameObject.scene.name}");
        }

        public virtual void Shutdown()
        {
            GameCore core = GameCore.GetInstance();
            core?.Get<UIManager>()?.ClearSceneReferences();
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

    }
}
