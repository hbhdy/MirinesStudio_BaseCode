using TMPro;
using UnityEngine;
using System.Collections;


namespace MirinesStudio
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        // ----- Param -----

        public string key;
        private TextMeshProUGUI txtMeshPro;
        private Coroutine waitRoutine;
        private bool subscribed;

        // ----- Init -----

        private void Awake()
        {
            TryGetComponent<TextMeshProUGUI>(out txtMeshPro);
        }

        private void OnEnable()
        {
            waitRoutine = StartCoroutine(WaitAndRefresh());
        }

        private void OnDisable()
        {
            Recylce();
        }

        private void Recylce()
        {
            if (waitRoutine != null)
            {
                StopCoroutine(waitRoutine);
                waitRoutine = null;
            }

            if (subscribed && GameCore.Instance != null && GameCore.LOCAL != null)
            {
                GameCore.LOCAL.OnLanguageChanged -= Refresh;
                subscribed = false;
            }
        }

        // ----- Set -----

        private IEnumerator WaitAndRefresh()
        {
            yield return new WaitUntil(() => GameCore.GetIsAllReady());

            GameCore.LOCAL.OnLanguageChanged += Refresh;
            subscribed = true;

            Refresh();
        }

        private void SetText(string key)
        {
            if (GameCore.Instance != null)
                txtMeshPro.text = GameCore.LOCAL.GetLocalizedString(key);
        }


        private void Refresh()
        {
            if (!string.IsNullOrEmpty(key))
                SetText(key);
        }

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

    }
}