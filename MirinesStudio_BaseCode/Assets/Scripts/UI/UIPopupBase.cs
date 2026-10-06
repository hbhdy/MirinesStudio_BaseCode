using UnityEngine;

namespace MirinesStudio
{
    public abstract class UIPopupBase : UIBase
    {
        // ----- Param -----

        [SerializeField]
        private bool closeOnBack = true;

        // ----- Init -----

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        public virtual bool HandleBack()
        {
            if (closeOnBack)
                CloseUI();

            return true;
        }

        // ----- Logic -----

    }
}
