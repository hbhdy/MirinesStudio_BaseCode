#if UNITY_EDITOR && SHOW_LOG
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MirinesStudio
{
    public class DevelopmentDebugPanel : MonoBehaviour
    {
        // ----- Param -----

        private Rect windowRect = new Rect(12, 12, 370, 360);
        private Vector2 scrollPosition;
        private readonly StringBuilder text = new StringBuilder(512);
        private string statusText = string.Empty;
        private string uiText = string.Empty;
        private string performanceText = "FPS: --    Frame: -- ms";
        private bool visible;
        private bool collapsed;
        private bool changedTimeScale;
        private float previousTimeScale;
        private float sampleTime;
        private int sampleFrames;
        private RectTransform pointerBlocker;
        private GameObject blockerRoot;

        // ----- Init -----

        private void Awake()
        {
            // IMGUI does not stop uGUI raycasts, so block only the panel rectangle.
            blockerRoot = new GameObject("DebugPanelInput", typeof(Canvas), typeof(GraphicRaycaster));
            blockerRoot.transform.SetParent(transform, false);
            Canvas canvas = blockerRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            GameObject blocker = new GameObject("PointerBlocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(blockerRoot.transform, false);
            pointerBlocker = blocker.GetComponent<RectTransform>();
            pointerBlocker.anchorMin = pointerBlocker.anchorMax = pointerBlocker.pivot = new Vector2(0, 1);
            blocker.GetComponent<Image>().color = Color.clear;
            blockerRoot.SetActive(false);
        }

        private void OnDisable()
        {
            visible = false;
            RestoreTimeScale();
            if (blockerRoot != null)
                blockerRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (blockerRoot != null)
                Destroy(blockerRoot);
        }

        // ----- Set -----

        private void SetVisible(bool active)
        {
            visible = active;
            blockerRoot.SetActive(active);
            if (active)
            {
                sampleTime = 0;
                sampleFrames = 0;
                performanceText = "FPS: --    Frame: -- ms";
                RefreshSnapshot();
                UpdateWindowBounds();
            }
            else
                RestoreTimeScale();
        }

        private void RefreshSnapshot()
        {
            GameCore core = GameCore.GetInstance();
            SceneLoadingManager scene = core != null ? core.Get<SceneLoadingManager>() : null;
            statusText = $"Core: {GameCore.State}    Scene: {SceneManager.GetActiveScene().name}\nLoading: {scene != null && scene.IsLoading}";
            text.Clear();
            UIManager ui = core != null ? core.Get<UIManager>() : null;
            if (ui != null)
                ui.AppendDebugInfo(text);
            else
                text.Append("UIManager: not registered");
            uiText = text.ToString();
        }

        private void SetTimeScale(float value)
        {
            if (!changedTimeScale)
            {
                previousTimeScale = Time.timeScale;
                changedTimeScale = true;
            }
            Time.timeScale = value;
        }

        private void RestoreTimeScale()
        {
            if (!changedTimeScale)
                return;
            Time.timeScale = previousTimeScale;
            changedTimeScale = false;
        }

        private void UpdateWindowBounds()
        {
            windowRect.width = Mathf.Min(370, Screen.width);
            windowRect.height = Mathf.Min(collapsed ? 62 : 360, Screen.height);
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, Screen.height - windowRect.height));
            pointerBlocker.anchoredPosition = new Vector2(windowRect.x, -windowRect.y);
            pointerBlocker.sizeDelta = windowRect.size;
        }

        // ----- Get -----

        // ----- Main -----

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                SetVisible(!visible);
            if (!visible)
                return;

            sampleTime += Time.unscaledDeltaTime;
            sampleFrames++;
            if (sampleTime >= 0.5f)
            {
                performanceText = $"FPS: {sampleFrames / sampleTime:F1}    Frame: {sampleTime * 1000f / sampleFrames:F1} ms";
                sampleTime = 0;
                sampleFrames = 0;
                RefreshSnapshot();
            }
            UpdateWindowBounds();
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            int previousDepth = GUI.depth;
            GUI.depth = -10000;
            windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "DEBUG  |  F1");
            GUI.depth = previousDepth;
            UpdateWindowBounds();
        }

        // ----- Logic -----

        private void DrawWindow(int id)
        {
            if (GUI.Button(new Rect(windowRect.width - 112, 2, 72, 20), collapsed ? "Expand" : "Collapse"))
                collapsed = !collapsed;
            if (GUI.Button(new Rect(windowRect.width - 36, 2, 28, 20), "X"))
                SetVisible(false);

            GUILayout.BeginArea(new Rect(10, 26, windowRect.width - 20, windowRect.height - 34));
            if (collapsed)
                GUILayout.Label(performanceText);
            else
            {
                GUILayout.Label(statusText);
                GUILayout.Label(performanceText);
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Mathf.Max(30, windowRect.height - 172)));
                GUILayout.Label(uiText);
                GUILayout.EndScrollView();
                GUILayout.Label($"Time scale: {Time.timeScale:0.##}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("x0.5")) SetTimeScale(0.5f);
                if (GUILayout.Button("x1")) SetTimeScale(1f);
                if (GUILayout.Button("x2")) SetTimeScale(2f);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
            GUI.DragWindow(new Rect(0, 0, Mathf.Max(0, windowRect.width - 116), 24));
        }
    }
}
#endif
