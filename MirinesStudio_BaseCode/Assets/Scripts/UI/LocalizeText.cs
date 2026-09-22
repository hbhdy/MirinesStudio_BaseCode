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

        // ----- Init -----

        private void Awake()
        {
            TryGetComponent<TextMeshProUGUI>(out txtMeshPro);
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(key))
                SetText(key);
        }

        // ----- Set -----

        private void SetText(string key)
        {
            if (GameCore.Instance != null)
                txtMeshPro.text = GameCore.LOCAL.GetLocalizedString(key);
        }

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

    }
}