using TMPro;
using UnityEngine;


namespace MirinesStudio
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        // ----- Param -----

        public string key;
        private TextMeshProUGUI txtMeshPro;
        private bool subscribed;

        // ----- Init -----

        private void Awake()
        {
            TryGetComponent<TextMeshProUGUI>(out txtMeshPro);
        }

        private void OnEnable()
        {
            GameCore.OnStateChanged += OnCoreStateChanged;
            TrySubscribeAndRefresh();
        }

        private void OnDisable()
        {
            Recylce();
        }

        private void Recylce()
        {
            GameCore.OnStateChanged -= OnCoreStateChanged;

            LocalizationManager localization = GameCore.LOCAL;
            if (subscribed && localization != null)
                localization.OnLanguageChanged -= Refresh;

            subscribed = false;
        }

        // ----- Set -----

        private void OnCoreStateChanged(GameCoreState state)
        {
            if (state == GameCoreState.Ready)
                TrySubscribeAndRefresh();
        }

        private void TrySubscribeAndRefresh()
        {
            if (subscribed || !GameCore.GetIsAllReady())
                return;

            LocalizationManager localization = GameCore.LOCAL;
            if (localization == null)
                return;

            localization.OnLanguageChanged += Refresh;
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
