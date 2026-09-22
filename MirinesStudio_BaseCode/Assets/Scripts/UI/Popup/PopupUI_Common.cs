using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MirinesStudio
{
    public class PopupUI_Common : UIBase
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

            btnDim.onClick.AddListener(CloseUI);
            btnClose.onClick.AddListener(CloseUI);
        }

        public void InitAutoClose()
        {
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
            base.CloseUI();

            callback?.Invoke();
        }

        // ----- Logic -----
    }
}