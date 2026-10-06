using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MirinesStudio
{
    public class PopupUI_Common : UIPopupBase
    {
        // ----- Param -----

        public override UIType UIType => UIType.PopupUI_Common;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI txtComment;

        [Header("button")]
        [SerializeField] private Button btnDim;
        [SerializeField] private Button btnClose;

        private Action callback = null;

        // ----- Init -----

        protected override void OnAwake()
        {
            base.OnAwake();

            if (btnDim != null)
                btnDim.onClick.AddListener(CloseUI);
            if (btnClose != null)
                btnClose.onClick.AddListener(CloseUI);
        }

        public void InitAutoClose()
        {
            if (btnDim != null)
                btnDim.interactable = false;
            Invoke("CloseUI", 2f);
        }

        // ----- Set -----

        public void SetTextKey(string key, bool isFormat = false, string str = "")
        {
            if (!isFormat)
                txtComment.text = GameCore.LOCAL.GetLocalizedString(key);
            else
                txtComment.text = string.Format(GameCore.LOCAL.GetLocalizedString(key), str);
        }

        // ----- Get -----

        // ----- Main -----

        public override void OpenUI()
        {
            base.OpenUI();
        }

        public override void CloseUI()
        {
            if (IsClosing || !gameObject.activeSelf)
                return;

            base.CloseUI();

            callback?.Invoke();
        }

        // ----- Logic -----
    }
}
